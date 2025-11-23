using RevloDB.Entities;

namespace RevloDB.Utility
{
    public static class RoleCheckUtil
    {
        public static bool HasSufficientRole(NamespaceRole NamespaceRole, NamespaceRole requiredRole)
        {
            return requiredRole switch
            {
                NamespaceRole.ReadOnly => NamespaceRole == NamespaceRole.ReadOnly || NamespaceRole == NamespaceRole.Editor || NamespaceRole == NamespaceRole.Admin,
                NamespaceRole.Editor => NamespaceRole == NamespaceRole.Editor || NamespaceRole == NamespaceRole.Admin,
                NamespaceRole.Admin => NamespaceRole == NamespaceRole.Admin,
                _ => false
            };
        }
    }
}