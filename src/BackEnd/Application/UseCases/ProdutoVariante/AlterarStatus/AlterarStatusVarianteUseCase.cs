using Application.Request;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.ProdutoVariante.AlterarStatus
{
    public class AlterarStatusVarianteUseCase : IAlterarStatusVarianteUseCase
    {
        private readonly IProdutoVarianteRepository _varianteRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public AlterarStatusVarianteUseCase(IProdutoVarianteRepository varianteRepository, ISalvarDBRepository salvarDB)
        {
            _varianteRepository = varianteRepository;
            _salvarDB = salvarDB;
        }

        public async Task Execute(int produtoId, int varianteId, RequestAlterarStatus request)
        {
            var variante = await _varianteRepository.GetById(produtoId, varianteId);

            if (variante is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.VARIANTE_NAO_ENCONTRADA);

            variante.Ativo = request.Ativo;

            _varianteRepository.Update(variante);

            await _salvarDB.Salvar();
        }
    }
}
