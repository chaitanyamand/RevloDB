using RevloDB.Constants;

namespace RevloDB.Utils
{
    public static class ControllerUtil
    {
        public static int GetNameSpaceIdFromHTTPContext(HttpContext httpContext)
        {
            return int.Parse(httpContext.GetItem<string>(APIConstants.NAMESPACE_ID)!);
        }
    }
}