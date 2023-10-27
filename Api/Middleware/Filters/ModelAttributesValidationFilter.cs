using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using API.v1.Models;

namespace API.Middleware.Filters
{
    public class ModelAttributesValidationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (!actionContext.ModelState.IsValid)
            {
                Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
                foreach (var modelState in actionContext.ModelState)
                {
                    errors.Add(modelState.Key, modelState.Value.Errors.Select(a => a.ErrorMessage).ToList());
                }
                actionContext.Result = new BadRequestResult();
            }
        }
    }
}
