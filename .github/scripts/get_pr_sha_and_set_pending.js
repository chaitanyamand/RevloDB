module.exports = async ({github, context, core}) => {
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
    context: process.env.CONTEXT_NAME,
    description: process.env.DESCRIPTION,
    target_url: `https://github.com/${context.repo.owner}/${context.repo.repo}/actions/runs/${process.env.RUN_ID}`
  });
};
