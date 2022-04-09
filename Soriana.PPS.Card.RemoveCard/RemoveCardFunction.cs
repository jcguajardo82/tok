using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Soriana.PPS.Common.DTO.Card;
using Soriana.PPS.Card.RemoveCard.Services;
using Soriana.PPS.Card.RemoveCard.Constants;
using Soriana.PPS.Common.Constants;
using Soriana.PPS.Common.DTO.Common;

namespace Soriana.PPS.Card.RemoveCard
{
    public class RemoveCardFunction
    {
        #region Public Fields
        private readonly ILogger<RemoveCardFunction> _Logger;
        private readonly IRemoveCardService _RemoveCardService;
        #endregion

        #region Constructors
        public RemoveCardFunction(ILogger<RemoveCardFunction> Logger, IRemoveCardService removeCardService)
        {
            _Logger = Logger;
            _RemoveCardService = removeCardService; 
        }
        #endregion

        #region Public Methods
        [FunctionName(RemoveCardConstants.REMOVE_CARD_FUNCTION_NAME)]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request)
        {
            try
            {
                _Logger.LogInformation(string.Format(FunctionAppConstants.FUNCTION_EXECUTING_MESSAGE, RemoveCardConstants.REMOVE_CARD_FUNCTION_NAME));
                if (!request.Body.CanSeek)
                    throw new Exception(JsonConvert.SerializeObject(new BusinessResponse() { StatusCode = (int)HttpStatusCode.BadRequest, Description = HttpStatusCode.BadRequest.ToString(), DescriptionDetail = RemoveCardConstants.REMOVE_CARD_NO_CONTENT_REQUEST, ContentRequest = null}));
                request.Body.Position = 0;
                string jsonRemoveCardRequest = await new StreamReader(request.Body).ReadToEndAsync();

                RemoveCardRequest RemoveCardRequest = JsonConvert.DeserializeObject<RemoveCardRequest>(jsonRemoveCardRequest);

                var Response = await _RemoveCardService.RemoveCard(RemoveCardRequest);

                return new OkObjectResult(new BusinessResponse() { StatusCode = (int)HttpStatusCode.OK, Description = HttpStatusCode.OK.ToString(), DescriptionDetail = RemoveCardConstants.REMOVE_CARD_FUNCTION_NAME, Object = Response });
            }
            catch (BusinessException ex)
            {
                _Logger.LogError(ex, RemoveCardConstants.REMOVE_CARD_FUNCTION_NAME);
                return new BadRequestObjectResult(ex);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, RemoveCardConstants.REMOVE_CARD_FUNCTION_NAME);
                return new BadRequestObjectResult(new BusinessResponse()
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Description = string.Concat(HttpStatusCode.InternalServerError.ToString(), CharactersConstants.ESPACE_CHAR, CharactersConstants.HYPHEN_CHAR, CharactersConstants.ESPACE_CHAR, RemoveCardConstants.REMOVE_CARD_FUNCTION_NAME),
                    DescriptionDetail = ex
                });
            }
        }
        #endregion
    }
}
