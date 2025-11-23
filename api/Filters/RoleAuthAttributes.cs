using RevloDB.Entities;

namespace RevloDB.Filters
{
    public abstract class RoleRequiredAttribute : Attribute
    {
        public abstract NamespaceRole RequiredRole { get; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ReadAttribute : RoleRequiredAttribute
    {
        public override NamespaceRole RequiredRole => NamespaceRole.ReadOnly;
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class WriteAttribute : RoleRequiredAttribute
    {
        public override NamespaceRole RequiredRole => NamespaceRole.Editor;
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class AdminAttribute : RoleRequiredAttribute
    {
        public override NamespaceRole RequiredRole => NamespaceRole.Admin;
    }
}