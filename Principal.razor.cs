
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;

public interface IPrincipal
{
    public Task<bool> DeterminarSeEprimo(BigInteger primo);
    public Task<long[]> GerarPrimosSequenciais();
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

    public async Task<long[]> GerarPrimosSequenciais()
    {
        //9.223.372.036.854.775.807
        

        var gerar = Task.Run(() =>
        {
            List<long> ar = new List<long>();
            long limit = 9223372036854775800;
            Parallel.For(3, limit, x =>
            {
                if(MillerRabinPrimo(new BigInteger(x),10)) ar.Add(x);
            });
            return ar;

        });

        bool finalizou = gerar.Wait(TimeSpan.FromSeconds(5));
        if (finalizou) return gerar.Result.OrderBy(x => x).ToArray();
        else return null;

    }
    
    public static bool MillerRabinPrimo(BigInteger primo, int precisao)
            {
                if(primo == 2 || primo == 3)
                    return true;
                if(primo < 2 || primo % 2 == 0)
                    return false;
     
                BigInteger d = primo - 1;
                int s = 0;
     
                while(d % 2 == 0)
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
}