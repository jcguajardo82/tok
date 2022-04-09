using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Soriana.PPS.Common.DTO.Card;
using Soriana.PPS.Card.SorianaApp.Services;
using Soriana.PPS.Card.SorianaApp.Constants;
using Soriana.PPS.Common.Constants;
using Soriana.PPS.Common.DTO.Common;

namespace Soriana.PPS.Card.SorianaApp
{
    public class SorianaAppFunction
    {
        #region Private Fields
        private readonly ILogger<SorianaAppFunction> _Logger;
        private readonly ISorianaAppService _SorianaAppService;
        #endregion

        #region Constructor
        public SorianaAppFunction(ILogger<SorianaAppFunction> logger,   
                                  ISorianaAppService sorianaAppService)
        {
            _Logger = logger;
            _SorianaAppService = sorianaAppService;
        }
        #endregion

        #region Public Methods
        [FunctionName(SorianaAppConstants.SORIANA_APP_ADD_CARD_FUNCTION_NAME)]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request)
        {
            try
            {
                AddCardResponse Response = new AddCardResponse();
                _Logger.LogInformation(string.Format(FunctionAppConstants.FUNCTION_EXECUTING_MESSAGE, SorianaAppConstants.SORIANA_APP_ADD_CARD_FUNCTION_NAME));
                if (!request.Body.CanSeek)
                    throw new Exception(JsonConvert.SerializeObject(new BusinessResponse() { StatusCode = (int)HttpStatusCode.BadRequest, Description = HttpStatusCode.BadRequest.ToString(), DescriptionDetail = SorianaAppConstants.SORIANA_APPADD_CARD_NO_CONTENT_REQUEST, ContentRequest = null }));
                request.Body.Position = 0;
                string jsonAddCardRequest = await new StreamReader(request.Body).ReadToEndAsync();

                AddCardSorianaAppRequest AddCardRequest = JsonConvert.DeserializeObject<AddCardSorianaAppRequest>(jsonAddCardRequest);

                if (AddCardRequest.action == "setDefaultCard")
                {
                    Response = await _SorianaAppService.UpdatePredeterminatedCard(AddCardRequest);
                }
                else
                {
                    Response = await _SorianaAppService.AddCard(AddCardRequest, jsonAddCardRequest);
                }
              
                return new OkObjectResult(new BusinessResponse() { StatusCode = (int)HttpStatusCode.OK, Description = HttpStatusCode.OK.ToString(), DescriptionDetail = SorianaAppConstants.SORIANA_APP_ADD_CARD_FUNCTION_NAME, Object = Response });
            }
            catch (BusinessException ex)
            {
                _Logger.LogError(ex, SorianaAppConstants.SORIANA_APP_ADD_CARD_FUNCTION_NAME);
                return new BadRequestObjectResult(ex);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, SorianaAppConstants.SORIANA_APP_ADD_CARD_FUNCTION_NAME);
                return new BadRequestObjectResult(new BusinessResponse()
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Description = string.Concat(HttpStatusCode.InternalServerError.ToString(), CharactersConstants.ESPACE_CHAR, CharactersConstants.HYPHEN_CHAR, CharactersConstants.ESPACE_CHAR, SorianaAppConstants.SORIANA_APP_ADD_CARD_FUNCTION_NAME),
                    DescriptionDetail = ex
                });
            }

        }
        #endregion

    }
}
