using System.Security.Cryptography;
using System.Text;
using RevloDB.Entities;

namespace RevloDB.Utility
{
    public static class CommitHashUtil
    {
        public static string ComputeCommitHash(
            string? parentHash,
            string? mergeParentHash,
            int authorUserId,
            DateTime timestamp,
            string message,
            List<(string KeyName, string? Value, ChangeAction Action)> sortedChanges)
        {
            var sb = new StringBuilder();
            sb.AppendLine(parentHash ?? "null");
            sb.AppendLine(mergeParentHash ?? "null");
            sb.AppendLine(authorUserId.ToString());
            sb.AppendLine(timestamp.ToUniversalTime().ToString("O"));
            sb.AppendLine(message);

            foreach (var (keyName, value, action) in sortedChanges.OrderBy(c => c.KeyName))
            {
                sb.Append(keyName).Append('\0').Append(value ?? "").Append('\0').AppendLine(action.ToString());
            }

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
