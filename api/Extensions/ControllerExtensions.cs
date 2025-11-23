using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RevloDB.Extensions
{
    public static class ControllerExtensions
    {
        public static BadRequestObjectResult BadRequestProblem(this ControllerBase controller, string detail)
        {
            return controller.BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = detail,
                Instance = controller.HttpContext.Request.Path
            });
        }

        public static NotFoundObjectResult NotFoundProblem(this ControllerBase controller, string detail)
        {
            return controller.NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = detail,
                Instance = controller.HttpContext.Request.Path
            });
        }

        public static ObjectResult ForbiddenProblem(this ControllerBase controller, string detail)
        {
            return new ObjectResult(new ProblemDetails
            {
                Status = 403,
                Title = "Forbidden",
                Detail = detail,
                Instance = controller.HttpContext.Request.Path
            })
            {
                StatusCode = 403
            };
        }

        public static UnauthorizedObjectResult UnauthorizedProblem(this ControllerBase controller, string detail)
        {
            return controller.Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = detail,
                Instance = controller.HttpContext.Request.Path
            });
        }

        public static BadRequestObjectResult ModelValidationProblem(this ControllerBase controller, ModelStateDictionary modelState)
        {
            var errors = modelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value?.Errors != null ? x.Value.Errors.Select(e => e.ErrorMessage) : Enumerable.Empty<string>())
                .ToList();

            var detail = errors.Count == 1 ? errors.First() : string.Join(", ", errors);

            return controller.BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = detail,
                Instance = controller.HttpContext.Request.Path
            });
        }

    }


}