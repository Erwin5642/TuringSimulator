---
id: concepts-loops-and-blank
category: concepts
title: Loops e o vazio no fim da fita
level_id:
---

Quando o comprimento da entrada é desconhecido, não dá para colocar um bloco de movimento por posição na esteira. O caminho certo é montar um loop: um fio que volta para um bloco que já rodou.

O material vazio não é inútil. Ele representa a ausência de peça, pode marcar o fim da entrada e também serve para remover um material. Configure uma condição para “vazio”: porta verdadeira sai do loop (aceitar, ajustar material no fim etc.); porta falsa continua avançando. Uma condição de vazio também ajuda a chegar ao início ou ao fim da entrada.

Um loop típico: mover, (talvez ajustar material), inspecionar vazio e, se não for vazio, voltar a mover. Sem a saída no vazio, a esteira entra em ciclo infinito.
