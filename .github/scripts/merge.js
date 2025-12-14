// .github/scripts/ci.js
module.exports = async ({ github, context, core }) => {
    const functions = {
        // 1. Check Commenter Permission
        checkPermissions: async () => {
            const commenter = context.payload.comment.user.login;
            const repo = context.repo.repo;
            const owner = context.repo.owner;

            console.log(`Checking permissions for: ${commenter}`);

            const { data: permission } = await github.rest.repos.getCollaboratorPermissionLevel({
                owner,
                repo,
                username: commenter,
            });

            console.log(`User permission level: ${permission.permission}`);

            if (permission.permission === 'read' || permission.permission === 'none') {
                throw new Error(`User ${commenter} does not have merge permissions (requires 'write' or higher).`);
            }

            console.log(`User ${commenter} has sufficient permissions`);
            return true;
        },

        // 2. Wait for Required Status Checks
        waitForStatus: async (requiredChecks, sha) => {
            const { owner, repo } = context.repo;

            console.log(`Waiting for status checks on SHA: ${sha}`);
            console.log(`Required checks: ${requiredChecks.join(', ')}`);

            const checkStatus = async (checkName) => {
                // Try Checks API (GitHub Actions)
                try {
                    const checks = await github.rest.checks.listForRef({
                        owner,
                        repo,
                        ref: sha,
                        check_name: checkName,
                    });
                    if (checks.data.check_runs.length > 0) {
                        const check = checks.data.check_runs[0];
                        return check.status === 'completed' && check.conclusion === 'success';
                    }
                } catch (err) {
                    // If Checks API fails, try Statuses API
                }

                // Try Statuses API (External CI)
                try {
                    const statuses = await github.rest.repos.listCommitStatusesForRef({
                        owner,
                        repo,
                        ref: sha,
                    });
                    const status = statuses.data.find(s => s.context === checkName);
                    if (status) {
                        return status.state === 'success';
                    }
                } catch (err) {
                    console.log(`Check ${checkName} not found yet`);
                }

                return false;
            };

            // Wait logic with timeout
            const maxTime = 15 * 60 * 1000; // 15 minutes
            const interval = 10000; // 10 seconds
            const start = Date.now();

            while (Date.now() - start < maxTime) {
                console.log(`\n[${new Date().toISOString()}] Checking statuses...`);

                const results = await Promise.all(
                    requiredChecks.map(name => checkStatus(name))
                );

                if (results.every(passed => passed)) {
                    console.log('All required checks passed!');
                    return 'success';
                }

                const pending = requiredChecks.filter((_, i) => !results[i]);
                if (pending.length > 0) {
                    console.log(`Still waiting for: ${pending.join(', ')}`);
                }

                await new Promise(resolve => setTimeout(resolve, interval));
            }

            throw new Error('Timeout waiting for required checks.');
        },

        // 3. Auto-Merge Pull Request
        autoMerge: async (prNumber) => {
            console.log(`Attempting to auto-merge PR #${prNumber}`);

            try {
                await github.rest.pulls.merge({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    pull_number: prNumber,
                    merge_method: 'merge'
                });

                console.log(`Successfully merged PR #${prNumber}`);
                return true;
            } catch (error) {
                console.error(`Failed to merge PR #${prNumber}:`, error.message);
                throw error;
            }
        }
    };

    return functions;
};