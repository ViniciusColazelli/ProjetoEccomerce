# Projeto Ecommerce

> Projeto Interdisciplinar do **Centro Universitário FAM**

E-commerce desenvolvido para uma **empresa real do segmento de vestuário**, com foco em aplicar conhecimentos de desenvolvimento de software na construção de uma solução baseada em necessidades reais de negócio.

**Tecnologias:** C# • .NET • PostgreSQL • React

## Primeiro administrador

O cadastro (`POST /api/cliente`) sempre cria o usuário com perfil `Cliente`, e não existe endpoint para promover alguém, de propósito: como o e-mail não é verificado, qualquer pessoa poderia se cadastrar com o e-mail do admin.

Para criar um administrador:

1. Cadastre a conta normalmente pela API.
2. No SQL Editor do Supabase (ou em qualquer cliente PostgreSQL), rode:

   ```sql
   UPDATE clientes SET perfil = 'Admin' WHERE email = 'email-do-admin@dominio.com';
   ```

3. Faça login de novo. O perfil vai dentro do token JWT, então um token emitido antes da alteração continua como `Cliente`.

## Testes de integração

Os testes rodam contra um PostgreSQL real (projeto Supabase **exclusivo para testes**), configurado em `appsettings.Testing.json` ou na variável `ConnectionStrings__ConnectionPostgreSql`. Antes de cada teste, todas as tabelas são esvaziadas pelo Respawn. Por isso, **nunca aponte os testes para um banco com dados reais**.
