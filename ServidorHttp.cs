using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

class ServidorHttp{


    private TcpListener Controlador {get;set;}
    private int Porta {get;set;}
    private int QtdeRequests {get;set;} 

    private SortedList<string,string> TiposMime { get; set; }

    public ServidorHttp(int porta = 8080){
        this.Porta = porta;
        this.PopularTiposMIME();
        try
        {
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor Http está rodando na porta {this.Porta}");
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{this.Porta}");
            Task servidorHttpTask = Task.Run(() => AguardarRequests());
            servidorHttpTask.GetAwaiter().GetResult();
        } catch (Exception e){
            Console.WriteLine($"Erro ao iniciar servidor na porta {this.Porta}: \n{e.Message}");
        }
    }

    private async Task AguardarRequests()
    {
        while(true)
        {
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdeRequests));
        }
    }

    private void ProcessarRequest(Socket conexao, int numeroRequest){
        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if(conexao.Connected)
        {
            byte[] byteRequisicao = new byte[1024];
            conexao.Receive(byteRequisicao, byteRequisicao.Length,0);
            string textoRequisicao = Encoding.UTF8.GetString(byteRequisicao).Replace((char)0, ' ').Trim();
            if (textoRequisicao.Length >0)
            {
                Console.WriteLine($"\n{textoRequisicao}\n");

                string[] linhas = textoRequisicao.Split("\r\n");

                int iPrimeiroEspaco = linhas[0].IndexOf(' ');
                int iSegundoEspaco = linhas[0].LastIndexOf(' ');
                string metodoHttp = linhas[0].Substring(0, iPrimeiroEspaco);
                string recursoBuscado = linhas[0].Substring(iPrimeiroEspaco+1,iSegundoEspaco-iPrimeiroEspaco-1);
                if (recursoBuscado == "/") recursoBuscado = "/index.html";
                recursoBuscado = recursoBuscado.Split("?")[0];
                string versaoHttp = linhas[0].Substring(iSegundoEspaco+1);
                iPrimeiroEspaco = linhas[1].IndexOf(' ');
                string nomeHost = linhas[1].Substring(iPrimeiroEspaco+1);
                byte[] bytesCabecalho;
                byte[] bytesConteudo;
                FileInfo fiArquivo = new FileInfo(ObterCaminhoArquivo(recursoBuscado));
                if (fiArquivo.Exists){
                    if (TiposMime.ContainsKey(fiArquivo.Extension.ToLower())){
                        bytesConteudo = File.ReadAllBytes(fiArquivo.FullName);
                        string tipoMime = TiposMime[fiArquivo.Extension.ToLower()];
                        bytesCabecalho = GerarCabecalho("HTTP/1.1", tipoMime, "200", bytesConteudo.Length);
                    }
                    else
                    {
                        bytesConteudo = Encoding.UTF8.GetBytes("<h1> Erro 415 - tipo de arquivo nao suportado </h1>");
                        bytesCabecalho = GerarCabecalho(versaoHttp,"text/http;charset=utf-8", "415", bytesConteudo.Length);
                    }
                }
                else
                {
                    bytesConteudo = LerArquivoNotFound();
                    bytesCabecalho = GerarCabecalho("HTTP/1.1", "text/html;charset=utf-8","404", bytesConteudo.Length);
                
                }
                
                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);
                conexao.Close();
                Console.WriteLine($"\n{bytesEnviados} bytes enviados em resposta à requisição ${numeroRequest}");
            }
            Console.WriteLine($"\nRequest {numeroRequest} finalizado");
        }
    }

    public byte[] GerarCabecalho(string versaoHttp, string tipoMime, string codigoHttp, int qtdeBytes = 0)
    {
        StringBuilder texto = new StringBuilder();
        texto.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
        texto.Append($"Server: Servidor Http Simples 1.0 {Environment.NewLine}");
        texto.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
        texto.Append($"Content-Lenght: {qtdeBytes}{Environment.NewLine}{Environment.NewLine}");
        return Encoding.UTF8.GetBytes(texto.ToString());
    }

    private byte[] LerArquivoNotFound()
    {
        var diretorio = System.IO.Directory.GetCurrentDirectory();
        string caminhoNotFound = $"{diretorio}{Path.DirectorySeparatorChar}www{Path.DirectorySeparatorChar}notfound.html";
        if (File.Exists(caminhoNotFound)) return File.ReadAllBytes(caminhoNotFound);
        else return new byte[0];
    }

    private void PopularTiposMIME()
    {
        this.TiposMime = new SortedList<string, string>();
        this.TiposMime.Add(".html","text/html;charset=utf-8");
        this.TiposMime.Add(".htm","text/html;charset=utf-8");
        this.TiposMime.Add(".css","text/css;");
        this.TiposMime.Add(".js","text/js;");
        this.TiposMime.Add(".png","text/png;");
        this.TiposMime.Add(".jpg","text/jpg;");
        this.TiposMime.Add(".gif","text/gif;");
        this.TiposMime.Add(".svg","text/svg+xml;");
        this.TiposMime.Add(".webp","text/webp;");
        this.TiposMime.Add(".ico","text/ico;");
        this.TiposMime.Add(".woff","text/woff;");
        this.TiposMime.Add(".woff2","text/woff2;");
    }


    private string ObterCaminhoArquivo(string nome){
        var diretorio = System.IO.Directory.GetCurrentDirectory();
        string caminho = $"{diretorio}{Path.DirectorySeparatorChar}www{Path.DirectorySeparatorChar}{nome}";
        return caminho;
    }

}