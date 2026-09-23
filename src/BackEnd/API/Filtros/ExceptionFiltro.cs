using Application.Response;
using Exceptions;
using Exceptions.ExceptionBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Net;

namespace API.Filtros
{
    public class ExceptionFiltro : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is EccomerceException)
            {
                LidarComException(context);
            }
            else
            {
                ThrowExceptionDesconhecida(context);
            }
        }

        private void LidarComException(ExceptionContext context)
        {
            if (context.Exception is ErroEmValidacaoException)
            {
                var exception = context.Exception as ErroEmValidacaoException;

                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Result = new BadRequestObjectResult(new ResponseErro(exception!.ErrorMessages));
            }
            else if (context.Exception is ClienteNaoLogadoException)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new UnauthorizedObjectResult(new ResponseErro(context.Exception.Message));
            }
            else if (context.Exception is ErroEmLoginException)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new UnauthorizedObjectResult(new ResponseErro(context.Exception.Message));
            }
        }

        private void ThrowExceptionDesconhecida(ExceptionContext context)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Result = new ObjectResult(new ResponseErro(ResourceMensagensDeErro.ERRO_DESCONHECIDO));
        }
    }
}
