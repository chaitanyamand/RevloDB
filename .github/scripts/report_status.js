module.exports = async ({github, context}) => {
  const state = {
    success: 'success',
    failure: 'failure',
    cancelled: 'error'
  }[process.env.JOB_STATUS] ?? 'error';
  
  await github.rest.repos.createCommitStatus({
    owner: context.repo.owner,
    repo: context.repo.repo,
    sha: process.env.SHA,
    state: state,
    context: process.env.CONTEXT_NAME,
    description: `${process.env.DESCRIPTION_PREFIX}: ${process.env.JOB_STATUS}`,
    target_url: `https://github.com/${context.repo.owner}/${context.repo.repo}/actions/runs/${process.env.RUN_ID}`
  });
};
