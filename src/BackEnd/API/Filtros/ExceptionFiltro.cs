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
        private readonly ILogger<ExceptionFiltro> _logger;

        public ExceptionFiltro(ILogger<ExceptionFiltro> logger) => _logger = logger;

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
            else if (context.Exception is NaoEncontradoException)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Result = new NotFoundObjectResult(new ResponseErro(context.Exception.Message));
            }
            else if (context.Exception is ConflitoException conflito)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
                context.Result = new ConflictObjectResult(new ResponseErro(conflito.ErrorMessages));
            }
        }

        private void ThrowExceptionDesconhecida(ExceptionContext context)
        {
            // O cliente recebe só "Erro desconhecido"; o detalhe fica no log do servidor (Render)
            _logger.LogError(context.Exception, "Erro não tratado em {Metodo} {Caminho}",
                context.HttpContext.Request.Method, context.HttpContext.Request.Path);

            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Result = new ObjectResult(new ResponseErro(ResourceMensagensDeErro.ERRO_DESCONHECIDO));
        }
    }
}
