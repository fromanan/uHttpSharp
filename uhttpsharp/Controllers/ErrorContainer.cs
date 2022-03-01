using System.Collections.Generic;
using System.Threading.Tasks;
using uhttpsharp.Handlers;

namespace uhttpsharp.Controllers
{
    public class ErrorContainer : IErrorContainer
    {
        private readonly IList<string> errors = new List<string>();

        public void Log(string description)
        {
            errors.Add(description);
        }

        public IEnumerable<string> Errors => errors;
        public bool Any => errors.Count != 0;
        public Task<IControllerResponse> GetResponse()
        {
            return
                Task.FromResult<IControllerResponse>(new RenderResponse(HttpResponseCode.MethodNotAllowed,
                    new { Message = string.Join(", ", errors) }));
        }
    }
}