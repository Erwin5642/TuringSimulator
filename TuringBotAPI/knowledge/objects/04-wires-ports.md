---
id: objects-wires-ports
category: objects
title: Fios e portas
level_id:
---

Blocos conversam por portas. Toda instrução tem uma porta de entrada (recebe o sinal) e uma ou duas portas de saída (passa o sinal adiante). Nem todo bloco tem duas saídas.

O fio vai sempre de uma saída para uma entrada. A ordem dos fios é a ordem em que o circuito executa os blocos.

Exemplos de regras:
- A tomada de energia só tem saída.
- O bloco de condição tem duas saídas: verdadeira e falsa. As duas precisam de fio.
- Aceitar e rejeitar só têm entrada. Não saem fios deles.

Se uma saída ficar solta, a energia não tem próximo fio e o programa encerra. Se os blocos Aceitar e Rejeitar já tiverem sido introduzidos, isso quer dizer que a máquina rejeitou a entrada.
