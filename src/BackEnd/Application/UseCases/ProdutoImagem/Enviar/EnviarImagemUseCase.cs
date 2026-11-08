using Application.Request;
using Application.Response;
using Application.Services.Imagens;
using Application.Services.Mapeamento;
using Domain.Entities;
using Domain.Extensions;
using Domain.Repositories;
using Domain.Services.Armazenamento;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.ProdutoImagem.Enviar
{
    // Grava a foto (e a miniatura) no Storage e registra no banco, no fim da fila: a primeira foto é a capa.
    // Se algo falhar no meio do caminho, apaga o que já foi gravado para não sobrar arquivo sem registro.
    public class EnviarImagemUseCase : IEnviarImagemUseCase
    {
        public const int MAXIMO_IMAGENS = 8;

        // o navegador reduz antes de enviar (até 1600 px e 480 px em WebP): passar disso é foto sem redução
        public const long TAMANHO_MAXIMO_IMAGEM = 3 * 1024 * 1024;
        public const long TAMANHO_MAXIMO_MINIATURA = 1 * 1024 * 1024;

        private readonly IProdutoRepository _produtoRepository;
        private readonly IProdutoImagemRepository _imagemRepository;
        private readonly ISalvarDBRepository _salvarDB;
        private readonly IArmazenamentoImagens _armazenamento;

        public EnviarImagemUseCase(IProdutoRepository produtoRepository, IProdutoImagemRepository imagemRepository, ISalvarDBRepository salvarDB, IArmazenamentoImagens armazenamento)
        {
            _produtoRepository = produtoRepository;
            _imagemRepository = imagemRepository;
            _salvarDB = salvarDB;
            _armazenamento = armazenamento;
        }

        public async Task<ResponseProdutoImagem> Execute(int produtoId, RequestEnviarImagem request)
        {
            var produtoExiste = await _produtoRepository.Existe(produtoId);

            if (produtoExiste.IsFalse())
                throw new NaoEncontradoException(ResourceMensagensDeErro.PRODUTO_NAO_ENCONTRADO);

            var imagens = await _imagemRepository.ListarPorProduto(produtoId);

            var (tipoImagem, tipoMiniatura) = await Validate(request, imagens.Count);

            // nome único: o arquivo nunca é sobrescrito, então o navegador pode guardar em cache para sempre
            var nome = Guid.NewGuid().ToString("N");
            var imagem = new ProdutoImagens
            {
                ProdutoId = produtoId,
                Caminho = $"produtos/{produtoId}/{nome}{tipoImagem.Extensao}",
                CaminhoMiniatura = $"produtos/{produtoId}/{nome}-mini{tipoMiniatura.Extensao}",
                Ordem = imagens.Count == 0 ? 0 : imagens.Max(atual => atual.Ordem) + 1
            };

            var gravados = new List<string>();
            try
            {
                await _armazenamento.Salvar(imagem.Caminho, request.Imagem!.Conteudo, tipoImagem.ContentType);
                gravados.Add(imagem.Caminho);

                await _armazenamento.Salvar(imagem.CaminhoMiniatura, request.Miniatura!.Conteudo, tipoMiniatura.ContentType);
                gravados.Add(imagem.CaminhoMiniatura);

                await _imagemRepository.Adicionar(imagem);
                await _salvarDB.Salvar();
            }
            catch
            {
                await ApagarSemFalhar(gravados);
                throw;
            }

            return MapearImagem.ParaResponse(imagem, _armazenamento);
        }

        private static async Task<(TipoImagem.Detectado Imagem, TipoImagem.Detectado Miniatura)> Validate(RequestEnviarImagem request, int quantidadeAtual)
        {
            if (request.Imagem is null || request.Miniatura is null || request.Imagem.Tamanho == 0 || request.Miniatura.Tamanho == 0)
                throw new ErroEmValidacaoException(new List<string> { ResourceMensagensDeErro.IMAGEM_OBRIGATORIA });

            var erros = new List<string>();

            if (request.Imagem.Tamanho > TAMANHO_MAXIMO_IMAGEM || request.Miniatura.Tamanho > TAMANHO_MAXIMO_MINIATURA)
                erros.Add(ResourceMensagensDeErro.IMAGEM_MUITO_GRANDE);

            var tipoImagem = await TipoImagem.Detectar(request.Imagem.Conteudo);
            var tipoMiniatura = await TipoImagem.Detectar(request.Miniatura.Conteudo);

            if (tipoImagem is null || tipoMiniatura is null)
                erros.Add(ResourceMensagensDeErro.IMAGEM_TIPO_INVALIDO);

            if (quantidadeAtual >= MAXIMO_IMAGENS)
                erros.Add(ResourceMensagensDeErro.PRODUTO_LIMITE_IMAGENS);

            if (erros.Count > 0)
                throw new ErroEmValidacaoException(erros);

            return (tipoImagem!, tipoMiniatura!);
        }

        // A falha que importa é a original: se a limpeza também falhar, sobra só um arquivo solto no Storage
        private async Task ApagarSemFalhar(List<string> caminhos)
        {
            if (caminhos.Count == 0)
                return;

            try
            {
                await _armazenamento.Apagar(caminhos);
            }
            catch
            {
                // ignora: a exceção relançada pelo Execute é a que explica o problema
            }
        }
    }
}
