using Application.Request;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.ProdutoOpcao.AlterarStatus
{
    public class AlterarStatusOpcaoUseCase : IAlterarStatusOpcaoUseCase
    {
        private readonly IProdutoOpcaoRepository _opcaoRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public AlterarStatusOpcaoUseCase(IProdutoOpcaoRepository opcaoRepository, ISalvarDBRepository salvarDB)
        {
            _opcaoRepository = opcaoRepository;
            _salvarDB = salvarDB;
        }

        public async Task Execute(int produtoId, int opcaoId, RequestAlterarStatus request)
        {
            var opcao = await _opcaoRepository.GetById(produtoId, opcaoId);

            if (opcao is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.OPCAO_NAO_ENCONTRADA);

            opcao.Ativo = request.Ativo;

            _opcaoRepository.Update(opcao);

            await _salvarDB.Salvar();
        }
    }
}
