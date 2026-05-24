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

        private static string GetRecursiveAncestorCteBody(string cteName, string paramPlaceholder) => $$"""
            SELECT id, parent_commit_id, merge_parent_commit_id, generation
            FROM commits WHERE id = {{paramPlaceholder}}

            UNION

            SELECT c.id, c.parent_commit_id, c.merge_parent_commit_id, c.generation
            FROM commits c
            INNER JOIN {{cteName}} ac ON c.id = ac.parent_commit_id OR c.id = ac.merge_parent_commit_id
            """;

        private static readonly string RecursiveAncestorCte = GetRecursiveAncestorCteBody("ancestor_chain", "{0}");

        public async Task<Commit?> GetNearestSnapshotAncestorAsync(int commitId)
        {
            var sql = $$"""
                WITH RECURSIVE ancestor_chain AS (
                    {{RecursiveAncestorCte}}
                )
                SELECT "Value" FROM (
                    SELECT ac.id AS "Value", ac.generation
                    FROM ancestor_chain ac
                    INNER JOIN commit_snapshots cs ON cs.commit_id = ac.id
                    ORDER BY ac.generation DESC, ac.id DESC
                    LIMIT 1
                ) t
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
                .ThenBy(c => c.Id)
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
                .ThenBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<Commit?> GetByHashAsync(string hash, int namespaceId)
        {
            return await _context.Commits
                .AsNoTracking()
                .Include(c => c.Snapshot)
                .Include(c => c.Changes)
                .FirstOrDefaultAsync(c => c.Hash == hash && c.NamespaceId == namespaceId);
        }

        public async Task<List<Commit>> GetHistoryAsync(int startCommitId, int limit)
        {
            var sql = $$"""
                WITH RECURSIVE ancestor_chain AS (
                    {{RecursiveAncestorCte}}
                )
                SELECT "Value" FROM (
                    SELECT id AS "Value", generation
                    FROM ancestor_chain
                    ORDER BY generation DESC, id DESC
                    LIMIT {1}
                ) t
                """;

            var ids = await _context.Database
                .SqlQueryRaw<int>(sql, startCommitId, limit)
                .ToListAsync();

            if (ids.Count == 0)
                return new List<Commit>();

            return await _context.Commits
                .AsNoTracking()
                .Include(c => c.AuthorUser)
                .Where(c => ids.Contains(c.Id))
                .OrderByDescending(c => c.Generation)
                .ThenByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task<Commit> CreateAsync(Commit commit)
        {
            _context.Commits.Add(commit);
            await _context.SaveChangesAsync();
            return commit;
        }

        public async Task<List<int>> FindLCACandidatesAsync(int commitId1, int commitId2)
        {
            var sql = $$"""
                WITH RECURSIVE ancestors1 AS (
                    {{GetRecursiveAncestorCteBody("ancestors1", "{0}")}}
                ),
                ancestors2 AS (
                    {{GetRecursiveAncestorCteBody("ancestors2", "{1}")}}
                )
                SELECT "Value" FROM (
                    SELECT a1.id AS "Value", a1.generation
                    FROM ancestors1 a1
                    INNER JOIN ancestors2 a2 ON a1.id = a2.id
                    GROUP BY a1.id, a1.generation
                    ORDER BY a1.generation DESC, a1.id DESC
                ) t
                """;

            var ids = await _context.Database
                .SqlQueryRaw<int>(sql, commitId1, commitId2)
                .ToListAsync();

            return ids;
        }
    }
}
