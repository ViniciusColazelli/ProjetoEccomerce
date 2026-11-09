namespace Application.Response
{
    // Resposta: corpo do 200 (sem token). Tokens: gravados nos cookies pelo controller
    public sealed record ResultadoLogin(ResponseClienteLogado Resposta, TokensSessao Tokens);
}
