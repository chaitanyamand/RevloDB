module.exports = async ({ github, context, core }) => {
    const functions = {
        // Check if commenter has merge permissions
        checkPermissions: async () => {
            const commenter = context.payload.comment.user.login;
            const { owner, repo } = context.repo;

            const { data: permission } = await github.rest.repos.getCollaboratorPermissionLevel({
                owner,
                repo,
                username: commenter,
            });

            if (permission.permission === 'read' || permission.permission === 'none') {
                throw new Error(`User ${commenter} does not have merge permissions (requires 'write' or higher).`);
            }

            return true;
        },

        // Get PR head SHA
        getPRHeadSHA: async (prNumber) => {
            const { owner, repo } = context.repo;

            try {
                const pr = await github.rest.pulls.get({
                    owner,
                    repo,
                    pull_number: prNumber,
                });

                return pr.data.head.sha;
            } catch (error) {
                throw new Error(`Could not fetch PR details: ${error.message}`);
            }
        },

        // Wait for required status checks to pass
        waitForStatus: async (requiredChecks, sha) => {
            const { owner, repo } = context.repo;
            const prNumber = context.payload.issue.number;

            // Fetch SHA from PR if not provided
            let commitSha = sha;
            if (!commitSha || commitSha.trim() === '') {
                commitSha = await functions.getPRHeadSHA(prNumber);
            }

            if (!commitSha || commitSha.trim() === '') {
                throw new Error('Could not determine commit SHA.');
            }

            const checkStatus = async (checkName) => {
                let foundInChecks = false;
                let foundInStatuses = false;

                // Check GitHub Actions (Checks API)
                try {
                    const checks = await github.rest.checks.listForRef({
                        owner,
                        repo,
                        ref: commitSha,
                        check_name: checkName,
                    });

                    if (checks.data.check_runs.length > 0) {
                        const check = checks.data.check_runs[0];
                        if (check.status === 'completed') {
                            foundInChecks = check.conclusion === 'success';
                        }
                    }
                } catch (err) {
                    // Silently handle error
                }

                // Check External CI (Statuses API)
                try {
                    const statuses = await github.rest.repos.listCommitStatusesForRef({
                        owner,
                        repo,
                        ref: commitSha,
                    });

                    const status = statuses.data.find(s => s.context === checkName);
                    if (status) {
                        foundInStatuses = status.state === 'success';
                    }
                } catch (err) {
                    // Silently handle error
                }

                return foundInChecks || foundInStatuses;
            };

            // Wait with timeout
            const maxTime = 5 * 60 * 1000; // 5 minutes
            const interval = 30000; // 30 seconds
            const start = Date.now();

            while (Date.now() - start < maxTime) {
                const results = await Promise.all(
                    requiredChecks.map(name => checkStatus(name))
                );

                if (results.every(passed => passed)) {
                    return 'success';
                }

                await new Promise(resolve => setTimeout(resolve, interval));
            }

            throw new Error('Timeout waiting for required checks after 15 minutes.');
        },

        // Auto-merge pull request
        autoMerge: async (prNumber) => {
            try {
                const pr = await github.rest.pulls.get({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    pull_number: prNumber,
                });

                if (pr.data.mergeable === false) {
                    throw new Error(`PR #${prNumber} is not mergeable. State: ${pr.data.mergeable_state}`);
                }

                await github.rest.pulls.merge({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    pull_number: prNumber,
                    merge_method: 'merge'
                });

                return true;
            } catch (error) {
                throw new Error(`Failed to merge PR #${prNumber}: ${error.message}`);
            }
        }
    };

    return functions;
};