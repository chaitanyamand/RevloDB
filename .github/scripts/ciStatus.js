module.exports = async ({ github, context, core }, statusContext, pendingDescription, completedDescription) => {
  const functions = {
    // Get PR Head SHA and set pending commit status
    getPRHeadSHAWithStatus: async () => {
      const pr = await github.rest.pulls.get({
        owner: context.repo.owner,
        repo: context.repo.repo,
        pull_number: context.issue.number,
      });

      const sha = pr.data.head.sha;
      core.setOutput('sha', sha);

      await github.rest.repos.createCommitStatus({
        owner: context.repo.owner,
        repo: context.repo.repo,
        sha: sha,
        state: 'pending',
        description: pendingDescription,
        context: statusContext,
      });
    },

    // Report final job status as a commit status
    reportStatus: async (sha, jobStatus) => {
      const state = jobStatus === 'success' ? 'success' : 'failure';

      await github.rest.repos.createCommitStatus({
        owner: context.repo.owner,
        repo: context.repo.repo,
        sha: sha,
        state: state,
        description: `${completedDescription}: ${state}`,
        context: statusContext,
      });
    },
  };

  return functions;
};