using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

class Servidor{
    private TcpListener Controlador {get;set;}
    private int Porta{get;set;}
    private int QtdeRequests{get;set;}
    public string Html {get;set;}

    public Servidor(int porta = 8080){
        this.Porta = porta;
        this.CriarHtml();
        try{
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor HTTP está rodando na porta {this.Porta}.");
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{this.Porta}.");
            Task servidorHttpTask = Task.Run(() => AguardarRequests());
            servidorHttpTask.GetAwaiter().GetResult();
        }
        catch (Exception e){
            Console.WriteLine($"Erro ao iniciar servidor na porta {this.Porta}: \n{e.Message}");
        }
    }

    private async Task AguardarRequests(){
        while(true){
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdeRequests));
        }
    }

    private void ProcessarRequest(Socket conexao, int numeroRequest){
        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if(conexao.Connected){
            byte[] bytesRequisicao = new byte[1024];
            conexao.Receive(bytesRequisicao, bytesRequisicao.Length, 0);
            string textoRequisicao = Encoding.UTF8.GetString(bytesRequisicao).Replace((char)0, ' ').Trim();

            if(textoRequisicao.Length > 0){
                Console.WriteLine($"\n{textoRequisicao}\n");

                string[] linhas = textoRequisicao.Split("\r\n");
                int iPrimeiroEspaco = linhas[0].IndexOf(' ');
                int iSegundoEspaco = linhas[0].LastIndexOf(' ');
                string metodoHttp = linhas[0].Substring(0, iPrimeiroEspaco);
                string recursoBuscado = linhas[0].Substring(iPrimeiroEspaco + 1, iSegundoEspaco - iPrimeiroEspaco - 1);
                string versaoHttp = linhas[0].Substring(iSegundoEspaco + 1);
                iPrimeiroEspaco = linhas[1].IndexOf(' ');
                string nomeHost =linhas[1].Substring(iPrimeiroEspaco + 1);

                byte[] bytesCabecalho = null;
                var bytesConteudo = LerArquivo(recursoBuscado);

                if(bytesConteudo.Length > 0){
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8",
                    "200", bytesConteudo.Length);
                }
                else{
                    bytesConteudo = Encoding.UTF8.GetBytes("<h1>Erro 404 - Arquivo Não Encontrado</h1>");
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8",
                    "404", bytesConteudo.Length);
                }

                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);
                conexao.Close();
                Console.WriteLine($"\n{bytesEnviados} bytes enviados em resposta à requisicao # {numeroRequest}.");
            }
        }
        Console.WriteLine($"\nRequest {numeroRequest} finalizado.");
    }

    private byte[] GerarCabecalho(string versaoHttp, string tipoMime, string codigoHttp, int qtdeBytes =0)
    {
        StringBuilder texto = new StringBuilder();
        texto.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
        texto.Append($"Server: Servidor Http Simples 1.0{Environment.NewLine}");
        texto.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
        texto.Append($"Content-Length: {qtdeBytes}{Environment.NewLine}{Environment.NewLine}");
        return Encoding.UTF8.GetBytes(texto.ToString());
    }

    private void CriarHtml(){
        StringBuilder html = new StringBuilder();
        html.Append("<!DOCTYP html><html lang=\"pt-br\"><head><meta charset=\"UTF-8\">");
        html.Append("<meta name =\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.Append("<title>Servidor Http Simples</title></head><body>");
        html.Append("<h1>Página Estática</h1></body></html>");
        this.Html = html.ToString();
    }

    public byte[] LerArquivo(string recurso){
        string diretorio = "C:\\Users\\eltho\\Documents\\DesenvolvimentoWeb\\Desenvolvimento C#\\Servidor_HTTP\\www";
        string caminhoArquivo = diretorio + recurso.Replace("/", "\\");
        if(File.Exists(caminhoArquivo)){
            return File.ReadAllBytes(caminhoArquivo);
        }
        else
            return new byte[0];
    }
}