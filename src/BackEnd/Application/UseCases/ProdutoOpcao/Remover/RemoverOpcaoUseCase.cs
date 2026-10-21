using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.ProdutoOpcao.Remover
{
    public class RemoverOpcaoUseCase : IRemoverOpcaoUseCase
    {
        private readonly IProdutoOpcaoRepository _opcaoRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public RemoverOpcaoUseCase(IProdutoOpcaoRepository opcaoRepository, ISalvarDBRepository salvarDB)
        {
            _opcaoRepository = opcaoRepository;
            _salvarDB = salvarDB;
        }

        public async Task Execute(int produtoId, int opcaoId)
        {
            var opcao = await _opcaoRepository.GetById(produtoId, opcaoId);

            if (opcao is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.OPCAO_NAO_ENCONTRADA);

            var emUso = await _opcaoRepository.EstaEmUso(opcao.Id);

            if (emUso)
                throw new ConflitoException(ResourceMensagensDeErro.OPCAO_EM_USO);

            _opcaoRepository.Remover(opcao);

            await _salvarDB.Salvar();
        }
    }
}
