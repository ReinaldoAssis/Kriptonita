
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ElectronNET.API;
using Kriptonita.Pages.Subpaginas;

public interface IPrincipal
{
    public Task<bool> DeterminarSeEprimo(BigInteger primo);
    public Task<string> GerarPrimosSequenciais(int timespan=3, bool paralelo=false, long limit=10000000);

    public Task<Dictionary<string,int>> FatorarEmPrimos(BigInteger numero);

    public Task<dynamic> MDC(List<BigInteger> ns); 
    public Task<dynamic> MMC(List<BigInteger> ns);

    public dynamic MDCEuclides(BigInteger a, BigInteger b);

    public dynamic EuclidesEstendido(long a, long b, string operacao = "linear", long congruencia=-1);

    public long RestoChines(List<string> equacoes);

    public void Limpeza(string contem);

    public BigInteger GerarNumeroPrimo();

    public void SalvarEmExecucao(string conteudo, string nome);

    public Task<string> LerEmExecucao(string nome);
}

public class Principal : IPrincipal
{
    public async Task<bool> DeterminarSeEprimo(BigInteger primo)
    {
        //BigInteger range = BigInteger.po;

        if (primo == new BigInteger(2) || primo == new BigInteger(3)) return true; //2 e 3 são primos
        if (primo % 2 == 0) return false; //números pares não são primos
        if (primo > 5 && primo % 5 == 0) return false; //números divisíveis por 5 não são primos
        if (primo < 2) return false; //números negativos, 1 ou 0 não são primos


        //utilizei metodos async para funções cujo processamento é demorado, são mais dificeis de implementar
        //e manter, porém evitam o congelamento do programa
        return await Task.Run(() => MillerRabinPrimo(primo, 10)); 
    }

    public async Task<string> GerarPrimosSequenciais(int timespan, bool paralelo, long limit)
    {
        //9.223.372.036.854.775.807 - limite long

        if (paralelo) return await GerarPrimosSequenciaisParalelo(timespan);
        else return await GerarPrimosSequenciaisSingle(timespan, limit: limit);
    }

    public async Task<string> GerarPrimosSequenciaisParalelo(int timespan)
    {
        string ar = "";
        DateTime t = DateTime.Now.AddSeconds(timespan);
        //long limit = 9223372036854775800;
        long limit = 775800;
        Parallel.For(3, limit, (x,loop) =>
        {
            if (MillerRabinPrimo(new BigInteger(x), 10)) ar += $"{x}, ";
            if(t.CompareTo(DateTime.Now) <= 0) loop.Stop();
        });
        return ar;
    }
    
    public async Task<string> GerarPrimosSequenciaisSingle(int timespan, long limit=10000000)
    {
        string ar = "";

        await Task.Run(() =>
        {
            ar = EratostenesPrimos(limit, timespan);
        });
        
        return ar;
    }

    public async Task<Dictionary<string,int>> FatorarEmPrimos(BigInteger numero)
    {
        Dictionary<string,int> fatores = new Dictionary<string, int>();
        BigInteger resto = numero;
        Stopwatch relogio = new Stopwatch();
        relogio.Start();
        
        Debug($"fatorando numero {numero}", "Fatorar em primos");
        
        while (resto != 1)
        {
            ChecarTimeout(relogio);
            if (resto % 2 == 0){ //se for divisível por 2 - primeiro primo

                if (fatores.ContainsKey("2")) fatores["2"] += 1; //se já existe o fator 2, adicione mais uma ocorrência
                else fatores.Add("2", 1); //caso não, adicione-o
                
                resto /= 2;
            }
            else //caso não seja, tentar sequencialmente em +2 (para sobrecarga de números pares)
            {
                BigInteger n = new BigInteger(3);
                while (resto % n != 0)
                {
                    n += 2;
                    ChecarTimeout(relogio, maxtime:15000);
                    bool ehprimo = MillerRabinPrimo(n, 20);
                    while (!ehprimo){
                        ChecarTimeout(relogio, maxtime:15000);
                        //Console.WriteLine($"resto: {resto} | n: {n}");
                        n += 2; //enquanto não for primo, incrementar de 2 em 2 - evita testar números pares
                        ehprimo = MillerRabinPrimo(n, 20);
                    }

                }
                //quando finalmente achar um primo que divide
                if (fatores.ContainsKey($"{n}")) fatores[$"{n}"] += 1; //incrementa uma ocorrência
                else fatores.Add($"{n}",1);//adiciona na lista de fatores
                
                resto /= n; //resto vai ser igual a divisão do resto por n (primo)
            }
            //Console.WriteLine(resto);
            
        }
        
        Debug($"primeiro fator: {fatores.First()}", "Fatorar em primos");


        return fatores;
    }

    public async Task<dynamic> MDC(List<BigInteger> ns)
    {
        List<Dictionary<string, int>> fatoresNs = new List<Dictionary<string, int>>();
        Dictionary<string, int> fatoresComuns = new Dictionary<string, int>();
        List<Dictionary<string, int>> imutavel = new List<Dictionary<string, int>>();
    
        foreach (var item in ns)
        {
            Dictionary<string, int> fatores = await FatorarEmPrimos(item);
            fatoresNs.Add(fatores);
            imutavel.Add(fatores);
        }

        Dictionary<string, int> inicial = new Dictionary<string, int>();
        inicial = fatoresNs.First();
        fatoresNs.Remove(inicial);
        foreach (var fator in inicial)
        {
            if (fatoresNs.TrueForAll(x => x.ContainsKey(fator.Key)))
            {
                int min = fator.Value;
                foreach (var lista in fatoresNs.Where(x => x.ContainsKey(fator.Key)))
                {
                    foreach (var comum in lista) min = Math.Min(comum.Value, min);
                }
                fatoresComuns.Add(fator.Key, min);
            }
        }
        
                
    
        BigInteger result = new BigInteger(1);
        foreach (var item in fatoresComuns)
        {
            Console.WriteLine(item.Key+" | "+item.Value);
            result *= BigInteger.Pow(BigInteger.Parse(item.Key), (int)item.Value); //para cada fator comum multiplicar elevado ao minimo expoente
        }

        dynamic obj = new ExpandoObject();
        obj.result = result;
        obj.listas = imutavel;

        return obj;
    
    }
    
    public async Task<dynamic> MMC(List<BigInteger> ns)
    {
        List<Dictionary<string, int>> fatoresNs = new List<Dictionary<string, int>>();

        foreach (var item in ns)
        {
            Dictionary<string, int> fatores = await FatorarEmPrimos(item);
            fatoresNs.Add(fatores);
        }

        Dictionary<string, int> resultante = new Dictionary<string, int>();

        foreach (var lista in fatoresNs)
        {
            foreach (var fator in lista)
            {
                if(resultante.ContainsKey(fator.Key) == false) resultante.Add(fator.Key, fator.Value); //se não existir, adicione
                else
                {
                    resultante[fator.Key] = Math.Max(resultante[fator.Key], fator.Value); //se exisistir, compare os expoentes e pegue o maior
                }
            }
        }        
                
    
        BigInteger result = new BigInteger(1);
        foreach (var item in resultante)
        {
            //Console.WriteLine("Fator: "+item.Key+" | "+item.Value);
            result *= BigInteger.Pow(BigInteger.Parse(item.Key), (int)item.Value); //para cada fator comum multiplicar elevado ao minimo expoente
        }

        dynamic obj = new ExpandoObject();
        obj.result = result;
        obj.listas = fatoresNs;
        return obj;

    }

    public dynamic MDCEuclides(BigInteger a, BigInteger b)
    {
        Stopwatch relogio = new Stopwatch();
        relogio.Start();

        BigInteger aux = a;
        a = BigInteger.Max(a, b);
        b = BigInteger.Min(aux, b);
        //Console.WriteLine($"max {a} min {b}");

        List<BigInteger> restos = new List<BigInteger>();

        aux = a % b;
        restos.Add(aux);

        if (aux == 0) return  new {result=BigInteger.Min(a, b),restos=new List<BigInteger>{BigInteger.Min(a,b)}};
        if (aux != 0)
        {
            while (b % aux != 0)
            {
                if(ChecarTimeout(relogio, 2000)) throw new WarningException("loop timeout em MDC Eucldies"); //evita uma repetição infinita
                BigInteger aux2 = aux;
                aux = b % aux;
                restos.Add(aux);
                b = aux2;
                //Console.WriteLine($"Resto {aux} Ultimo {b}");
            }
        }
        

        relogio.Stop();

        dynamic obj = new {result=aux,restos=restos};
        return obj;
    }

    public dynamic EuclidesEstendido(long a, long b, string operacao="linear", long congruencia=-1) //'linear' retornar x e y da combinação linear, 'inverso' retorna o inverso do mod
    {
        // long aux = a;
        //
        // a = Math.Max(a, b);
        // b = Math.Min(aux, b);

        dynamic result = new {a=a,b=b,mdc=-1,x=-1,y=-1}; //objeto dinamico

        
        dynamic linear(long? novoA=null, long? novoB=null)
        {
            long x0=1, xn = 1;
            long y0=0, yn = 0;
            long x1 = 0;
            long y1 = 1;
            
            //long aux = a;

            long a1;
            long b1; 
            if (novoA == null)
            {
                a1 = Math.Max(a, b);
                b1 = Math.Min(b,a);
            }
            else
            {
                a1 = Math.Max(novoA??a, novoB??b);
                b1 = Math.Min(novoA??a, novoB??b);
            }

            long r = a1 % b1;
            long q = -2410;


            while (r > 0)
            {
                q = (long)Math.Floor((float)a1 / (float)b1);
                xn = x0 - q * x1;
                yn = y0 - q * y1;
                
                Console.WriteLine($"{xn} {x0} {x1}");

                x0 = x1;
                y0 = y1;
                x1 = xn;
                y1 = yn;
                a1 = b1;
                b1 = r;
                r = a1 % b1;

            }
            
            return new {a=Math.Max(a,b),b=Math.Min(a,b),x=xn,y=yn,mdc=b1};

        }

        dynamic inverso(long? novoA=null, long? novoB=null)
        {
            dynamic _result;
            novoA ??= a;
            novoB ??= b;
            
            Console.WriteLine($"inverso a {novoA} b {novoB}");
            
            _result = linear(novoA:novoA,novoB:novoB);
            
            if(novoB == 1) return new {inverso=new {existe=true,valor=0},a=a,b=b,x=_result.x,y=_result.y,mdc=_result.mdc};
            if(novoA == 1) return new {inverso=new {existe=true,valor=1},a=a,b=b,x=_result.x,y=_result.y,mdc=_result.mdc};
            
            if (_result.mdc != 1){ //se não forem primos relativos
                
                return new {inverso=new {existe=false,valor=_result.b-_result.y},a=a,b=b,x=_result.x,y=_result.y,mdc=_result.mdc};
            }

            long _valor = 0;
            if (novoB == _result.b) _valor = (_result.x < 0 ? novoB + _result.x : _result.x);
            else _valor = (_result.y < 0 ? novoB + _result.y : _result.y);
            
            return new {inverso=new {existe=true,valor=_valor},a=a,b=b,x=_result.x,y=_result.y,mdc=_result.mdc};
        }

        //idealmente seria usado o teorema de euler para congruencias lineares, o cálculo seria imensamente mais eficaz
        //mas ainda estou estudando o teorema e não saberia implementa-lo.
        dynamic congruenciaLinear()
        {
            dynamic _result = inverso(); //primeiro achamos o inverso modular
            Stopwatch stop = new Stopwatch();
            stop.Start();
            
            //simplificar congruencia
            long novoA = a;
            long novoB = b;
            long AMmdc = MDCEuclides((int) a, (int) b).result; //mdc entre a e modulo

            bool existe = congruencia % AMmdc == 0;
            
            while (novoA % AMmdc == 0 && novoB%AMmdc==0 && congruencia%AMmdc==0 && AMmdc != 1) //TODO: analisar se é necessário mudar para mdc de a e modulo
            {
                novoA /= AMmdc;
                novoB /= AMmdc;
                congruencia /= AMmdc;
                _result = inverso(novoA:novoA,novoB:novoB);
                Console.WriteLine($"{novoA} mod {novoB} = {_result.inverso.valor}");
                if(ChecarTimeout(stop, 3000)) throw new TimeoutException("Timeout na simplificacao da congruencia");
            }
            
            List<Eucldies.Result.Congruencia> retornos = new List<Eucldies.Result.Congruencia>();
            if (existe) //mudar de posição para antes da simplificação!
            {
                long xCongruencia = _result.inverso.valor*congruencia;
                xCongruencia %= novoB; //mod b
                Console.WriteLine($"A {novoA} B {congruencia} mod {novoB} xC {xCongruencia} inverso {_result.inverso.valor}");
                for (int i = 0; i < AMmdc; i++)
                {
                    retornos.Add(new Eucldies.Result.Congruencia(congruencia*_result.mdc*AMmdc,xCongruencia+((i)*novoB),true,A:novoA+((i-1)*_result.mdc),B:novoB+((i-1)*_result.mdc)));
                }
                // retorno.congruencia = congruencia;
                // retorno.xCongruencia = xCongruencia;
                // retorno.existe = true; //se o mdc divide o b  //congruencia % _result.mdc == 0
                return new {inverso=_result.inverso,a=a,b=b,x=_result.x,y=_result.y,mdc=_result.mdc, congruencias=retornos};
            }
            return new {inverso=_result.inverso,a=a,b=b,x=_result.x,y=_result.y,mdc=_result.mdc, congruencias=retornos};

        }
        
        switch (operacao.ToLower())
        {
            case "linear":
                return linear();
                break;
            
            case "inverso":
                return inverso();
                break;
            
            case "congruencia":
                return congruenciaLinear();
                break;
            
        }

        return result;

    }

    public long RestoChines(List<string> equacoes)
    {
        long total = 0; //o X que queremos encontrar
        
        Console.WriteLine("entrei");
        
        List<dynamic> valores = new List<dynamic>(); //lista das congruências lineares

        foreach (var eq in equacoes)
        {
            string[] xside = eq.Contains("=") ? eq.Split("=") : eq.Split("≡");
            string[] sides = xside[1].Split("mod");
            long mod = long.Parse(sides[1]); //pega o mod
            long b = long.Parse(sides[0]); //pega o b
            long a = long.Parse(xside[0].Replace("x", ""));

            long mdc = MDCEuclides((int)a, (int)mod).result; //TODO: MUDAR PARA LONG!!

            while (a % mdc == 0 && mod % mdc == 0 && b % mdc == 0 && mdc != 1) //simplifica a congruência
            {
                a /= mdc;
                mod /= mdc;
                b /= mdc;
            }
            
            Console.WriteLine($"a {a} b {b} mod {mod}");

            valores.Add(new {b=b,mod=mod});
            
            
        }

        long N = 1;
        valores.ForEach((x) => N *= (long)x.mod); //calcula o N total

        foreach (var eq in valores)
        {
            long n = N / eq.mod;
            long x = EuclidesEstendido(n, eq.mod,"inverso").inverso.valor;
            Console.WriteLine($"b {eq.b} mod {eq.mod} n {n} x {x} N {N}");
            // valores.Remove(eq);
            // valores.Add(new {b=eq.b,mod=eq.mod,x=x,n=n});
            total += n * x * (long)eq.b;
        }

        total %= N;
        
        return total;
    }

    static void ChecarTimeout(Stopwatch relogio, int? maxtime=null)
    {
        if (relogio.ElapsedMilliseconds > (maxtime ?? 3000)) throw new TimeoutException("Error ao fatorar primos, timeout."); //se está dentro do loop a mais de 1 minuto, quebrar.
    }
    
    static bool ChecarTimeout(Stopwatch relogio, int mili)
    {
        if (relogio.ElapsedMilliseconds > mili){
            Console.WriteLine("Eratostenes passou do tempo limite!");
            return true; //passou o tempo
        }
        else return false;
    }

    public static string EratostenesPrimos(long limite, int segundos)
    {
        bool primeiroSave = true;
        string file = Path.GetTempPath()+"Primos"+Guid.NewGuid().ToString()+".kriptonita";
        void SalvarPrimos(List<int> lista)
        {
            if (primeiroSave)
            {
                using (var x = File.CreateText(file))
                {
                    x.WriteLine(string.Join(", ",lista));
                }

                primeiroSave = false;
            }
            else
            {
                using (var x = File.AppendText(file))
                {
                    x.WriteLine(string.Join(", ",lista));
                }
            }
        }

        bool ChecarESalvar(List<int> lista) //salva primos no arquivo e limpa da memoria RAM
        {
            if (lista.Count > 100000)
            {
                SalvarPrimos(lista);
                return true;
            }
            else
            {
                return false;
            }
        }

        bool[] Crivo = new bool[limite+1];
        List<int> primos = new List<int>();

        Stopwatch relogio = new Stopwatch();
        relogio.Start();
        
        for (int i = 0; i <= limite; i++) Crivo[i] = true; //inicialmente todos os números são primos

        for (int i = 2; i * i <= limite; i++)
        {
            if (Crivo[i])
            {
                primos.Add(i);
                if (ChecarESalvar(primos)) primos.Clear();
                for (int j = i * i; j <= limite; j += i) Crivo[j] = false; //multiplos de i não são primos
            }
        }
        
        Console.WriteLine($"Main: {relogio.Elapsed.Milliseconds}");

        for (int i = (int)Math.Ceiling(Math.Sqrt(limite))-2; i <= limite; i++)
        {
            if(Crivo[i]) primos.Add(i);
            if (ChecarESalvar(primos)) primos.Clear();
        }
        SalvarPrimos(primos);
        
        relogio.Stop();
        Console.WriteLine($"Str: {relogio.Elapsed.Milliseconds}");


        return file;
    }

    public static bool MillerRabinPrimo(BigInteger primo, int precisao)
            {
                if(primo == 2 || primo == 3)
                    return true;
                if(primo < 2 || primo % 2 == 0)
                    return false;
     
                BigInteger d = primo - 1;
                int s = 0;
     
                while(d % 2 == 0) //fatora até não ser divisível por 2
                {
                    d /= 2;
                    s += 1;
                }
     
                //BigInteger não possui função propria para gerar números aleatorios
                //aqui um num. aleatorio é criado a partir de um array de bytes
                RandomNumberGenerator rng = RandomNumberGenerator.Create();
                byte[] bytes = new byte[primo.ToByteArray().LongLength];
                BigInteger a;
     
                for(int i = 0; i < precisao; i++)
                {
                    do
                    {
                        rng.GetBytes(bytes);
                        a = new BigInteger(bytes);
                    }
                    while(a < 2 || a >= primo - 2);
     
                    BigInteger x = BigInteger.ModPow(a, d, primo);
                    if(x == 1 || x == primo - 1)
                        continue;
     
                    for(int r = 1; r < s; r++)
                    {
                        x = BigInteger.ModPow(x, 2, primo);
                        if(x == 1)
                            return false;
                        if(x == primo - 1)
                            break;
                    }
     
                    if(x != primo - 1)
                        return false;
                }
     
                return true;
            }

    //usado para limpar arquivos armazenados no diretorio temporario
    public void Limpeza(string contem)
    {
        string pt = Path.GetTempPath();
        var files = Directory.GetFiles(pt, "*.kriptonita").Where(x => x.ToLower().Contains(contem));
        foreach (var f in files) if(File.Exists(f)) File.Delete(f);
    }

    public const bool debug = true;
    public void Debug(object message, string? reference=null)
    {
        if (debug)
        {
            Console.Write(message);
            Console.WriteLine($" | referencia: {reference ?? "null"}");
        }
    }

    public BigInteger GerarNumeroPrimo()
    {
        Stopwatch relogio = new Stopwatch();
        BigInteger n = new BigInteger();
        
        relogio.Start();

        while (true)
        {
            if (ChecarTimeout(relogio, 2000)) return -1;
            var rng = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[16];
            rng.GetBytes(buffer);
            n = new BigInteger(buffer); //numero aleatorio
            if (MillerRabinPrimo(n, 25))
            {
                return n;
            }
        }

    }

    public async void SalvarEmExecucao(string conteudo, string nome)
    {
        string path = await Electron.App.GetAppPathAsync();
        Console.WriteLine("PATH ->"+Path.Join(path,$"/{nome}.txt"));
        await File.WriteAllTextAsync(Path.Join(path,$"/{nome}.txt"),conteudo);
    }

    public async Task<string> LerEmExecucao(string nome)
    {
        string path = await Electron.App.GetAppPathAsync();
        return File.ReadAllText(Path.Join(path, $"/{nome}.txt"));

    }
}