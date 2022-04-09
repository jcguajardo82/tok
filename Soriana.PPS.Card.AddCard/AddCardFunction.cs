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
using Soriana.PPS.Card.AddCard.Services;
using Soriana.PPS.Card.AddCard.Constants;
using Soriana.PPS.Common.Constants;
using Soriana.PPS.Common.DTO.Common;


namespace Soriana.PPS.Card.AddCard
{
    public class AddCardFunction
    {
        #region Private Fields
        private readonly IAddCardService _AddCardService;
        private readonly ILogger _Logger;
        #endregion

        #region Constructor
        public AddCardFunction(IAddCardService addCardService,
                               ILogger<AddCardService> logger)
        {
            _AddCardService = addCardService;
            _Logger = logger;
        }
        #endregion

        #region Public Methods
        [FunctionName(AddCardConstants.ADD_CARD_FUNCTION_NAME)]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request)
        {
            try
            {
                AddCardResponse Response = new AddCardResponse();
                _Logger.LogInformation(string.Format(FunctionAppConstants.FUNCTION_EXECUTING_MESSAGE, AddCardConstants.ADD_CARD_FUNCTION_NAME));
                if (!request.Body.CanSeek)
                    throw new Exception(JsonConvert.SerializeObject(new BusinessResponse() { StatusCode = (int)HttpStatusCode.BadRequest, Description = HttpStatusCode.BadRequest.ToString(), DescriptionDetail = AddCardConstants.ADD_CARD_NO_CONTENT_REQUEST, ContentRequest = null }));
                request.Body.Position = 0;
                string jsonAddCardRequest = await new StreamReader(request.Body).ReadToEndAsync();

                AddCardRequest AddCardRequest = JsonConvert.DeserializeObject<AddCardRequest>(jsonAddCardRequest);

                if (AddCardRequest.action == "setDefaultCard")
                {
                    Response = await _AddCardService.UpdatePredeterminatedCard(AddCardRequest);
                }
                else
                {
                    Response = await _AddCardService.AddCard(AddCardRequest, jsonAddCardRequest);
                }

                //Response = await _AddCardService.AddCard(AddCardRequest, jsonAddCardRequest);

                return new OkObjectResult(new BusinessResponse() { StatusCode = (int)HttpStatusCode.OK, Description = HttpStatusCode.OK.ToString(), DescriptionDetail = AddCardConstants.ADD_CARD_FUNCTION_NAME, Object = Response });
            }
            catch (BusinessException ex)
            {
                _Logger.LogError(ex, AddCardConstants.ADD_CARD_FUNCTION_NAME);
                return new BadRequestObjectResult(ex);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, AddCardConstants.ADD_CARD_FUNCTION_NAME);
                return new BadRequestObjectResult(new BusinessResponse()
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Description = string.Concat(HttpStatusCode.InternalServerError.ToString(), CharactersConstants.ESPACE_CHAR, CharactersConstants.HYPHEN_CHAR, CharactersConstants.ESPACE_CHAR, AddCardConstants.ADD_CARD_FUNCTION_NAME),
                    DescriptionDetail = ex
                });
            }
        }
        #endregion
    }
}
