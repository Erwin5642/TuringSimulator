---
id: objects-accept-reject
category: objects
title: Circuitos aceitar e rejeitar
level_id:
---

Aceitar e rejeitar fazem o programa parar e retornar verdadeiro ou falso. Servem para dizer se o programa aprova ou não a entrada que chegou na esteira. O que chega nesses módulos é o sinal do circuito, não a peça.

Aceitar (carimbo verde): verdadeiro — a entrada passou.

Rejeitar (bin vermelho): falso — a entrada falhou.

Os dois só têm porta de entrada. Depois deles não continua nada.

Se a energia fica sem saída no circuito, o programa encerra. A esteira não manda a peça para Aceitar nem para Rejeitar. Essa parada não é um Aceitar: só existe aceite se o sinal chegar num circuito Aceitar; caso contrário a máquina conta o encerramento como rejeitar.
