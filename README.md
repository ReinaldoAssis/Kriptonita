# Kriptonita

<p align="center"><img src="Kriptonita.jpg"/></p>

Projeto de matemática discreta visando o apredizado - na prática - das aplicações da criptografia. Este projeto é construido a partir de uma aplicação Blazor Net utilizando o container ElectronJS (portado para .Net - ElectronNET), fazendo uso da MudBlazor como framework UI. Contudo, não foi utilizada nenhuma função - ou biblioteca - externa para atingir os resultados esperados do projeto, quisquer componentes externos apenas são utilizados para UX ou UI. Isto pode ser facilmente conferido ao observar o código principal do projeto, onde se localizam todas as funções pertinentes.

## Screenshots

![Home](/zsrc/ferramentas.jpg?raw=true "Home")
![Congruência Linear](/zsrc/euclides.jpg?raw=true "Congruência Linear")

## Funcionamento

Deliberadamente centralizei todo o código do programa - relacionado a computação requisitada no projeto - em um arquivo único chamado "Principal.razor.cs". É possível ignorar a extensão "razor" e tratar esse arquivo unicamente como um programa C#. Assim, para inspeção basta se referir a dado arquivo. Aqui detalharei algumas implementações principais:

#### Primalidade

Neste primeiro desafio logo ficou claro que checar individualmente todos os números ímpares entre 2 e n - como são implementados os algoritmos mais básicos - não seria nem remotamente eficiente (mesmo quando considerando apenas números de 2 até sqrt n). Com um pouco mais de pesquisa, porém, encontrei um algoritmo comumente referido como sendo o mais eficiente e capaz de computar grandes números, o teste de primalidade Miller-Rabin. Não entrarei em detalhes no funcionamento matemático, mas funciona basicamente reescrevendo n em uma forma exponencial e fatorando tal expoente até não ser divisível por dois, ou seja ser ímpar. Quando isso ocorre, testa-se se n divide ao menos um dos fatores e caso o faça é tido como um número primo. Este método não é perfeito porém, temos que executá-lo múltiplas vezes para garantir que a probabilidade de n não ser primo seja negligível. Vi em uma fonte que utilizando 40 tentativas (algo que é executado absurdamente rápido), mesmo se a cada segundo tivéssemos executado o algoritmo desde o início do universo ainda assim teríamos uma chance de apenas 1 em 1 milhão de estarmos errados! Então a falha do algoritmo é completamente indiferente – podemos trata-lo como perfeito assumindo um número de tentativas suficientemente alto.

Além de sua eficácia, a implementação que fiz em meu código utiliza a classe C# Bigintegers, com ela é possível utilizar números não limitados aos 64 bits que os inteiros normalmente possuem. Utilizando o site https://bigprimes.org/ gerei um primo de 300 dígitos e meu programa foi capaz de determinar sua primalidade em questão de microssegundos. Não foi fácil estudar o teste Miller-Rabin para implementa-lo, mas estou muito satisfeito com os resultados.

#### Primos sequenciais em 60 segundos

Esse desafio em questão é elaborado a testar a eficiência do nosso código. Dessa forma, foi possivelmente o desafio mais complicado para mim. Iniciei testando ímpares de 3 à infinito utilizando o teste Miller-Rabin discutido anteriormente. Contudo, múltiplos problemas surgiram ao seguir esta estratégia. O primeiro e mais importante era a limitação de memoria RAM para uma dada variável, é necessário armazenar todos os milhões de números gerados para que seja possível mostrar o resultado ao final dos 60 segundos. O Segundo problema estava relacionado com a complexidade do algoritmo, apesar de Miller-Rabin ser um bom teste de primalidade, não era optimo para esta tarefa em especifico. Assim, decidi utilizar o Crivo de Eratóstenes, um método de complexidade quase linear e muito eficiente para a tarefa em questão. Além de implementar esse método fiz algumas otimizações especiais ao meu programa, irei ressaltar apenas uma delas: gerenciamento de RAM. Não é viável armazenar todos os números gerados em uma variável – irá ficar claro quando for mostrado os resultados -, então decidi usar uma stream que cria e escreve em um arquivo temporário na pasta Temp (padrão Windows). Esse arquivo irá conter todos os números gerados em ordem, portanto basta apresenta-lo ao usuário no final dos 60 segundos. Com essa metodologia, fui capaz de atingir primos nas ordens de 1 bilhão e 500 milhões.

## NOTE

Projeto desenvolvido por Reinaldo M. Assis - UFAL. Sinta-se livre para clonar e analizar o projeto você mesmo.
