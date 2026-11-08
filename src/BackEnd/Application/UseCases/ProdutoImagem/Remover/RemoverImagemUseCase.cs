using Domain.Repositories;
using Domain.Services.Armazenamento;
using Exceptions;
using Exceptions.ExceptionBase;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ProdutoImagem.Remover
{
    // Apaga primeiro do banco e depois os arquivos. Se o Storage falhar, a foto já sumiu da loja
    // e sobra só um arquivo solto (fica no log); o contrário deixaria a loja apontando para foto inexistente.
    public class RemoverImagemUseCase : IRemoverImagemUseCase
    {
        private readonly IProdutoImagemRepository _imagemRepository;
        private readonly ISalvarDBRepository _salvarDB;
        private readonly IArmazenamentoImagens _armazenamento;
        private readonly ILogger<RemoverImagemUseCase> _logger;

        public RemoverImagemUseCase(IProdutoImagemRepository imagemRepository, ISalvarDBRepository salvarDB, IArmazenamentoImagens armazenamento, ILogger<RemoverImagemUseCase> logger)
        {
            _imagemRepository = imagemRepository;
            _salvarDB = salvarDB;
            _armazenamento = armazenamento;
            _logger = logger;
        }

        public async Task Execute(int produtoId, int imagemId)
        {
            var imagem = await _imagemRepository.GetById(produtoId, imagemId);

            if (imagem is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.IMAGEM_NAO_ENCONTRADA);

            _imagemRepository.Remover(imagem);

            await _salvarDB.Salvar();

            try
            {
                await _armazenamento.Apagar([imagem.Caminho, imagem.CaminhoMiniatura]);
            }
            catch (Exception erro)
            {
                _logger.LogWarning(erro, "Foto {ImagemId} do produto {ProdutoId} saiu do banco, mas os arquivos {Caminho} e {Miniatura} não foram apagados do Storage",
                    imagemId, produtoId, imagem.Caminho, imagem.CaminhoMiniatura);
            }
        }
    }
}
