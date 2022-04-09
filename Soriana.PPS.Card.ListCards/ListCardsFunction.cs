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
using Soriana.PPS.Card.ListCards.Services;
using Soriana.PPS.Card.ListCards.Constants;
using Soriana.PPS.Common.Constants;
using Soriana.PPS.Common.DTO.Common;

namespace Soriana.PPS.Card.ListCards
{
    public class ListCardsFunction
    {
        #region Private Fields
        private readonly IListCardsService _ListCardsService;
        private readonly ILogger<ListCardsFunction> _Logger;
        #endregion

        #region Constructor
        public ListCardsFunction(ILogger<ListCardsFunction> logger,
                                IListCardsService listCardsService)
        {
            _ListCardsService = listCardsService;
            _Logger = logger;
        }
        #endregion

        #region Public Methods
        [FunctionName(ListCardsConstants.LIST_CARDS_FUNCTION_NAME)]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request)
        {
            try
            {
                _Logger.LogInformation(string.Format(FunctionAppConstants.FUNCTION_EXECUTING_MESSAGE, ListCardsConstants.LIST_CARDS_FUNCTION_NAME));
                if (!request.Body.CanSeek)
                    throw new Exception(JsonConvert.SerializeObject(new BusinessResponse() { StatusCode = (int)HttpStatusCode.BadRequest, Description = HttpStatusCode.BadRequest.ToString(), DescriptionDetail = ListCardsConstants.LIST_CARDS_NO_CONTENT_REQUEST, ContentRequest = null }));
                request.Body.Position = 0;
                string jsonListCardsRequest = await new StreamReader(request.Body).ReadToEndAsync();

                ListCardRequest ListCardRequest = JsonConvert.DeserializeObject<ListCardRequest>(jsonListCardsRequest);

                var Response = await _ListCardsService.GetCards(ListCardRequest.customerId);

                return new OkObjectResult(new BusinessResponse() { StatusCode = (int)HttpStatusCode.OK, Description = HttpStatusCode.OK.ToString(), DescriptionDetail = ListCardsConstants.LIST_CARDS_FUNCTION_NAME, Object = Response });
            }
            catch (BusinessException ex)
            {
                _Logger.LogError(ex, ListCardsConstants.LIST_CARDS_FUNCTION_NAME);
                return new BadRequestObjectResult(ex);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, ListCardsConstants.LIST_CARDS_FUNCTION_NAME);
                return new BadRequestObjectResult(new BusinessResponse()
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Description = string.Concat(HttpStatusCode.InternalServerError.ToString(), CharactersConstants.ESPACE_CHAR, CharactersConstants.HYPHEN_CHAR, CharactersConstants.ESPACE_CHAR, ListCardsConstants.LIST_CARDS_FUNCTION_NAME),
                    DescriptionDetail = ex
                });
            }
        }
        #endregion
    }
}
