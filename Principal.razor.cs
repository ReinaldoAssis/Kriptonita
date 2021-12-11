﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;

public interface IPrincipal
{
    public Task<bool> DeterminarSeEprimo(BigInteger primo);
    public Task<string> GerarPrimosSequenciais(int timespan=3, bool paralelo=true, long limit=10000000);

    public Task<Dictionary<string,int>> FatorarEmPrimos(BigInteger numero);

    public Task<BigInteger> MDC(List<BigInteger> ns); 
    public Task<BigInteger> MMC(List<BigInteger> ns); 

    public void Limpeza(string contem);
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


        //não tenho certeza se async é realmente necessário aqui
        //mas já que estamos lidando com processamentos gigantescos, é melhor prevenir
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
                    ChecarTimeout(relogio);
                    bool ehprimo = MillerRabinPrimo(n, 20);
                    while (!ehprimo){
                        ChecarTimeout(relogio);
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

        return fatores;
    }

    public async Task<BigInteger> MDC(List<BigInteger> ns)
    {
        List<Dictionary<string, int>> fatoresNs = new List<Dictionary<string, int>>();
        Dictionary<string, int> fatoresComuns = new Dictionary<string, int>();
    
        foreach (var item in ns)
        {
            fatoresNs.Add(await FatorarEmPrimos(item));
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
    
        return result;
    
    }
    
    public async Task<BigInteger> MMC(List<BigInteger> ns)
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
    
        return result;
    
    }

    static void ChecarTimeout(Stopwatch relogio)
    {
        if (relogio.ElapsedMilliseconds > 3000) throw new TimeoutException("Error ao fatorar primos, timeout."); //se está dentro do loop a mais de 1 minuto, quebrar.
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

        bool ChecarESalvar(List<int> lista)
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
}