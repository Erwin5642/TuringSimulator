---
id: objects-tape-and-head
category: objects
title: Esteira, fita e braço mecânico
level_id:
---

A esteira é a fita da Máquina de Turing. Cada slot é uma posição na esteira (e corresponde a uma célula na fita). O braço mecânico (cabeçote) atua em uma posição na esteira por vez.

O braço mecânico só lê, coloca e remove material na posição que está debaixo dele. Para acessar outra posição na esteira, o circuito desloca a esteira.

A fita é infinita e, fora da entrada do nível, está preenchida com material vazio. Esse vazio não é “nada inútil”: é a referência para saber onde a entrada termina e para controlar loops com segurança.
