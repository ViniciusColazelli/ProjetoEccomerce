using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Enums;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.ProdutoOpcao.Registrar
{
    public class RegistrarOpcaoUseCase : IRegistrarOpcaoUseCase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IProdutoOpcaoRepository _opcaoRepository;
        private readonly ISalvarDBRepository _salvarDBRepository;

        public RegistrarOpcaoUseCase(IProdutoRepository produtoRepository, IProdutoOpcaoRepository opcaoRepository, ISalvarDBRepository salvarDBRepository)
        {
            _produtoRepository = produtoRepository;
            _opcaoRepository = opcaoRepository;
            _salvarDBRepository = salvarDBRepository;
        }

        public async Task<ResponseProdutoOpcaoAdmin> Execute(int produtoId, RequestRegistrarOpcao request)
        {
            var produtoExiste = await _produtoRepository.Existe(produtoId);

            if (produtoExiste.IsFalse())
                throw new NaoEncontradoException(ResourceMensagensDeErro.PRODUTO_NAO_ENCONTRADO);

            await Validate(produtoId, request);

            var opcao = MapearRequest.RequestParaEntidade(request, produtoId);

            await _opcaoRepository.Adicionar(opcao);

            await _salvarDBRepository.Salvar();

            return new ResponseProdutoOpcaoAdmin
            {
                Id = opcao.Id,
                Tipo = opcao.Tipo,
                Valor = opcao.Valor,
                PrecoAdicional = opcao.PrecoAdicional,
                Ativo = opcao.Ativo
            };
        }

        private async Task Validate(int produtoId, RequestRegistrarOpcao request)
        {
            var validator = new RegistrarOpcaoValidator();

            var result = validator.Validate(request);

            var tipoValido = Enum.TryParse(request.Tipo, ignoreCase: true, out TipoOpcao tipo);

            if (tipoValido && string.IsNullOrWhiteSpace(request.Valor).IsFalse())
            {
                var opcaoExiste = await _opcaoRepository.ExisteOpcao(produtoId, tipo, request.Valor);
                if (opcaoExiste)
                    result.Errors.Add(new FluentValidation.Results.ValidationFailure(string.Empty, ResourceMensagensDeErro.OPCAO_JA_CADASTRADA));
            }

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}
