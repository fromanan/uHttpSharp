using System.Collections.Generic;
using System.Threading.Tasks;
using uhttpsharp.Handlers;

namespace uhttpsharp.Controllers
{
    public class ErrorContainer : IErrorContainer
    {
        private readonly IList<string> _errors = new List<string>();

        public void Log(string description)
        {
            _errors.Add(description);
        }

        public IEnumerable<string> Errors => _errors;
        public bool Any => _errors.Count != 0;
        public Task<IControllerResponse> GetResponse()
        {
            return
                Task.FromResult<IControllerResponse>(new RenderResponse(HttpResponseCode.MethodNotAllowed,
                    new { Message = string.Join(", ", _errors) }));
        }
    }
}