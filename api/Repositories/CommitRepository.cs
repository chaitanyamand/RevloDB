using Microsoft.EntityFrameworkCore;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Repositories.Interfaces;

namespace RevloDB.Repositories
{
    public class CommitRepository : ICommitRepository
    {
        private readonly RevloDbContext _context;

        public CommitRepository(RevloDbContext context)
        {
            _context = context;
        }

        public async Task<Commit?> GetByIdAsync(int commitId)
        {
            return await _context.Commits
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == commitId);
        }

        public async Task<Commit?> GetByIdWithSnapshotAsync(int commitId)
        {
            return await _context.Commits
                .AsNoTracking()
                .Include(c => c.Snapshot)
                .FirstOrDefaultAsync(c => c.Id == commitId);
        }

        private const string RecursiveAncestorCte = """
            SELECT id, parent_commit_id, generation
            FROM commits WHERE id = {0}

            UNION ALL

            SELECT c.id, c.parent_commit_id, c.generation
            FROM commits c
            INNER JOIN ancestor_chain ac ON c.id = ac.parent_commit_id
            """;

        public async Task<Commit?> GetNearestSnapshotAncestorAsync(int commitId)
        {
            var sql = $$"""
                WITH RECURSIVE ancestor_chain AS (
                    {{RecursiveAncestorCte}}
                )
                SELECT ac.id AS "Value"
                FROM ancestor_chain ac
                INNER JOIN commit_snapshots cs ON cs.commit_id = ac.id
                ORDER BY ac.generation DESC
                LIMIT 1
                """;

            var ids = await _context.Database
                .SqlQueryRaw<int>(sql, commitId)
                .ToListAsync();

            if (ids.Count == 0)
                return null;

            return await _context.Commits
                .AsNoTracking()
                .Include(c => c.Snapshot)
                .FirstOrDefaultAsync(c => c.Id == ids[0]);
        }

        public async Task<List<Commit>> GetAncestorChainAsync(int fromCommitId, int toAncestorCommitId)
        {
            var sql = $$"""
                WITH RECURSIVE ancestor_chain AS (
                    {{RecursiveAncestorCte}}
                )
                SELECT id AS "Value" FROM ancestor_chain WHERE id != {1}
                """;

            var ids = await _context.Database
                .SqlQueryRaw<int>(sql, fromCommitId, toAncestorCommitId)
                .ToListAsync();

            if (ids.Count == 0)
                return new List<Commit>();

            return await _context.Commits
                .AsNoTracking()
                .Include(c => c.Changes)
                .Where(c => ids.Contains(c.Id))
                .OrderBy(c => c.Generation)
                .ToListAsync();
        }

        public async Task<List<Commit>> GetAncestorChainToRootAsync(int commitId)
        {
            var sql = $$"""
                WITH RECURSIVE ancestor_chain AS (
                    {{RecursiveAncestorCte}}
                )
                SELECT id AS "Value" FROM ancestor_chain
                """;

            var ids = await _context.Database
                .SqlQueryRaw<int>(sql, commitId)
                .ToListAsync();

            if (ids.Count == 0)
                return new List<Commit>();

            return await _context.Commits
                .AsNoTracking()
                .Include(c => c.Changes)
                .Where(c => ids.Contains(c.Id))
                .OrderBy(c => c.Generation)
                .ToListAsync();
        }
    }
}
