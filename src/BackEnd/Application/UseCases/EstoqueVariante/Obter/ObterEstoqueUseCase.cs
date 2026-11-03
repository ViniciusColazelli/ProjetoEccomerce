using Application.Response;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.EstoqueVariante.Obter
{
    public class ObterEstoqueUseCase : IObterEstoqueUseCase
    {
        private readonly IProdutoVarianteRepository _varianteRepository;

        public ObterEstoqueUseCase(IProdutoVarianteRepository varianteRepository)
        {
            _varianteRepository = varianteRepository;
        }

        public async Task<ResponseEstoqueVariante> Execute(int varianteId)
        {
            var variante = await _varianteRepository.ObterComEstoque(varianteId);

            if (variante is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.VARIANTE_NAO_ENCONTRADA);

            return new ResponseEstoqueVariante
            {
                VarianteId = variante.Id,
                ProdutoId = variante.ProdutoId,
                Produto = variante.Produto.Nome,
                Tamanho = variante.TamanhoOpcao.Valor,
                Cor = variante.CorOpcao?.Valor,
                Ativo = variante.Ativo,
                Quantidade = variante.Estoque?.Quantidade ?? 0
            };
        }
    }
}
