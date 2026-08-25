---
id: concepts-factory-metaphor
category: concepts
title: Fábrica e Máquina de Turing
level_id:
---

A fábrica é uma Máquina de Turing encarnada.

- A esteira é a fita.
- O braço mecânico é o cabeçote: ele lê e manipula uma posição na esteira por vez.
- Os circuitos e os fios formam a função de transição: dado o “estado” (onde o sinal está no circuito) e o material lido, o sistema coloca ou remove material, avança a esteira e segue por um fio.
- Aceitar e rejeitar são os estados finais. O conjunto de fitas que terminam em Aceitar é a linguagem que o circuito reconhece.

Fios não são enfeite: são as setas do diagrama de estados. Loops no fio são ciclos no diagrama. Colocar ou remover materiais na fita é memória externa — autômato finito não tem isso; a máquina da fábrica tem.
