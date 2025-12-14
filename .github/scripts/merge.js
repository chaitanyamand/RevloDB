module.exports = async ({ github, context, core }) => {
    const functions = {
        // 1. Check Commenter Permission
        checkPermissions: async () => {
            const commenter = context.payload.comment.user.login;
            const repo = context.repo.repo;
            const owner = context.repo.owner;

            console.log(`Checking permissions for: ${commenter}`);
            console.log(`Repository: ${owner}/${repo}`);

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

        // 2. Get PR Head SHA (new function)
        getPRHeadSHA: async (prNumber) => {
            console.log(`\n========================================`);
            console.log(`Fetching PR #${prNumber} details...`);
            console.log(`========================================`);

            const { owner, repo } = context.repo;

            try {
                const pr = await github.rest.pulls.get({
                    owner,
                    repo,
                    pull_number: prNumber,
                });

                const headSha = pr.data.head.sha;
                const baseSha = pr.data.base.sha;

                console.log(`PR Head SHA: ${headSha}`);
                console.log(`PR Base SHA: ${baseSha}`);
                console.log(`PR Head Ref: ${pr.data.head.ref}`);
                console.log(`PR Base Ref: ${pr.data.base.ref}`);
                console.log(`PR State: ${pr.data.state}`);
                console.log(`PR Mergeable: ${pr.data.mergeable}`);
                console.log(`PR Mergeable State: ${pr.data.mergeable_state}`);
                console.log(`PR Title: ${pr.data.title}`);

                return headSha;
            } catch (error) {
                console.error(`Failed to fetch PR #${prNumber}:`);
                console.error(`Error: ${error.message}`);
                throw new Error(`Could not fetch PR details: ${error.message}`);
            }
        },

        // 3. Wait for Required Status Checks (updated)
        waitForStatus: async (requiredChecks, sha) => {
            const { owner, repo } = context.repo;
            const prNumber = context.payload.issue.number;

            // If SHA is not provided, fetch it from the PR
            let commitSha = sha;
            if (!commitSha || commitSha.trim() === '') {
                console.log('\nSHA not provided, fetching from PR...');
                commitSha = await functions.getPRHeadSHA(prNumber);
            }

            // Validate SHA
            if (!commitSha || commitSha.trim() === '') {
                throw new Error('Could not determine commit SHA. Please check PR exists and has a valid head commit.');
            }

            if (!commitSha.match(/^[a-f0-9]{40}$/)) {
                console.warn(`SHA format may be invalid: ${commitSha}`);
            }

            console.log(`\n========================================`);
            console.log(`DEBUG: Starting status check monitoring`);
            console.log(`========================================`);
            console.log(`Repository: ${owner}/${repo}`);
            console.log(`Commit SHA: ${commitSha}`);
            console.log(`SHA Length: ${commitSha.length}`);
            console.log(`Required checks: ${requiredChecks.join(', ')}`);
            console.log(`Event payload comment body: "${context.payload.comment.body}"`);
            console.log(`PR Number: ${prNumber}`);

            // Add debug function to list ALL available statuses/checks
            const listAllStatuses = async () => {
                console.log('\n--- DEBUG: Listing ALL statuses and checks ---');

                try {
                    // List Statuses API
                    const statuses = await github.rest.repos.listCommitStatusesForRef({
                        owner,
                        repo,
                        ref: commitSha,
                    });

                    console.log(`\nSTATUSES API (context field):`);
                    if (statuses.data.length === 0) {
                        console.log('  No statuses found');
                    } else {
                        statuses.data.forEach((status, i) => {
                            console.log(`  [${i}] ${status.context}: ${status.state}`);
                        });
                    }
                } catch (err) {
                    console.log(`  Statuses API error: ${err.message}`);
                }

                try {
                    // List Checks API
                    const checks = await github.rest.checks.listForRef({
                        owner,
                        repo,
                        ref: commitSha,
                    });

                    console.log(`\nCHECKS API (name field):`);
                    if (checks.data.total_count === 0) {
                        console.log('  No checks found');
                    } else {
                        checks.data.check_runs.forEach((check, i) => {
                            console.log(`  [${i}] ${check.name}: ${check.status} (${check.conclusion || 'pending'})`);
                            console.log(`      App: ${check.app?.name || 'N/A'}`);
                        });
                    }
                } catch (err) {
                    console.log(`  Checks API error: ${err.message}`);
                }

                console.log('--- End of status list ---\n');
            };

            // Call debug function immediately
            await listAllStatuses();

            const checkStatus = async (checkName) => {
                console.log(`\nChecking: "${checkName}"`);
                let foundInChecks = false;
                let foundInStatuses = false;

                // Try Checks API (GitHub Actions)
                try {
                    const checks = await github.rest.checks.listForRef({
                        owner,
                        repo,
                        ref: commitSha,
                        check_name: checkName,
                    });

                    console.log(`  Checks API query results: ${checks.data.total_count} check(s) found`);

                    if (checks.data.check_runs.length > 0) {
                        const check = checks.data.check_runs[0];
                        console.log(`  Found in Checks API as: "${check.name}"`);
                        console.log(`  Status: ${check.status}, Conclusion: ${check.conclusion || 'N/A'}`);
                        console.log(`  App: ${check.app?.name || 'N/A'}`);

                        if (check.status === 'completed') {
                            foundInChecks = check.conclusion === 'success';
                        } else {
                            console.log(`  Check not completed yet`);
                        }
                    } else {
                        console.log(`  Check "${checkName}" not found in Checks API`);
                    }
                } catch (err) {
                    console.log(`  Checks API query error: ${err.message}`);
                }

                // Try Statuses API (External CI)
                try {
                    const statuses = await github.rest.repos.listCommitStatusesForRef({
                        owner,
                        repo,
                        ref: commitSha,
                    });

                    const status = statuses.data.find(s => s.context === checkName);
                    if (status) {
                        console.log(`  Found in Statuses API as: "${status.context}"`);
                        console.log(`  State: ${status.state}, Description: ${status.description || 'N/A'}`);
                        foundInStatuses = status.state === 'success';
                    } else {
                        console.log(`  Not found in Statuses API as "${checkName}"`);
                        // List what contexts are actually available
                        const availableContexts = statuses.data.map(s => s.context);
                        if (availableContexts.length > 0) {
                            console.log(`  Available contexts: ${availableContexts.join(', ')}`);
                        }
                    }
                } catch (err) {
                    console.log(`  Statuses API error: ${err.message}`);
                }

                if (!foundInChecks && !foundInStatuses) {
                    console.log(`  Check "${checkName}" not found in either API`);
                    return false;
                }

                const result = foundInChecks || foundInStatuses;
                console.log(`  Overall result for "${checkName}": ${result ? 'PASS' : 'PENDING'}`);
                return result;
            };

            // Wait logic with timeout
            const maxTime = 15 * 60 * 1000; // 15 minutes
            const interval = 30000; // 30 seconds
            const start = Date.now();
            let iteration = 0;

            while (Date.now() - start < maxTime) {
                iteration++;
                console.log(`\n========================================`);
                console.log(`Iteration ${iteration} - ${new Date().toISOString()}`);
                console.log(`Elapsed: ${Math.round((Date.now() - start) / 1000)}s`);
                console.log(`========================================`);

                const results = await Promise.all(
                    requiredChecks.map(name => checkStatus(name))
                );

                console.log(`\nSummary for iteration ${iteration}:`);
                requiredChecks.forEach((name, i) => {
                    console.log(`  ${name}: ${results[i] ? 'PASS' : 'PENDING'}`);
                });

                if (results.every(passed => passed)) {
                    console.log('\nAll required checks passed!');
                    return 'success';
                }

                const pending = requiredChecks.filter((_, i) => !results[i]);
                if (pending.length > 0) {
                    console.log(`\nStill waiting for: ${pending.join(', ')}`);

                    // List all statuses again every 3 iterations to see if new ones appeared
                    if (iteration % 3 === 0) {
                        await listAllStatuses();
                    }
                }

                console.log(`\nWaiting ${interval / 1000}s before next check...`);
                await new Promise(resolve => setTimeout(resolve, interval));
            }

            // Final debug before timeout
            await listAllStatuses();

            console.log('\nTIMEOUT REACHED');
            console.log(`Required checks that never passed:`);
            requiredChecks.forEach(check => {
                console.log(`  - ${check}`);
            });

            throw new Error('Timeout waiting for required checks after 15 minutes.');
        },

        // 4. Auto-Merge Pull Request
        autoMerge: async (prNumber) => {
            console.log(`\n========================================`);
            console.log(`Attempting to auto-merge PR #${prNumber}`);
            console.log(`========================================`);

            try {
                // First, get PR info for debugging
                const pr = await github.rest.pulls.get({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    pull_number: prNumber,
                });

                console.log(`PR Title: ${pr.data.title}`);
                console.log(`PR State: ${pr.data.state}`);
                console.log(`PR Mergeable: ${pr.data.mergeable}`);
                console.log(`PR Mergeable State: ${pr.data.mergeable_state}`);
                console.log(`PR Head SHA: ${pr.data.head.sha}`);

                // Check if PR is mergeable
                if (pr.data.mergeable === false) {
                    throw new Error(`PR #${prNumber} is not mergeable. Mergeable state: ${pr.data.mergeable_state}`);
                }

                // Attempt merge
                const result = await github.rest.pulls.merge({
                    owner: context.repo.owner,
                    repo: context.repo.repo,
                    pull_number: prNumber,
                    merge_method: 'merge'
                });

                console.log(`\nSuccessfully merged PR #${prNumber}`);
                console.log(`Merge SHA: ${result.data.sha}`);
                console.log(`Merged Message: ${result.data.message}`);
                return true;
            } catch (error) {
                console.error(`\nFailed to merge PR #${prNumber}:`);
                console.error(`Error: ${error.message}`);
                if (error.response) {
                    console.error(`Status: ${error.response.status}`);
                    console.error(`Data: ${JSON.stringify(error.response.data, null, 2)}`);
                }
                throw error;
            }
        }
    };

    return functions;
};