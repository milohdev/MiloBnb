# Checkpoints de Verificación — MiloBnb

## Build y Verificación
- [ ] dotnet build sin errores ni warnings
- [ ] docker compose up levanta api + postgres (2 servicios)
- [ ] Endpoints responden correctamente en Swagger
- [ ] Seed data se carga al iniciar (Admin por defecto)

## Arquitectura
- [ ] Controllers solo despachan via MediatR, cero lógica
- [ ] Endpoints reciben y devuelven DTOs, nunca entidades de dominio
- [ ] Interfaces en Application, implementaciones en Infrastructure
- [ ] Domain no referencia ningún otro proyecto

## Código
- [ ] No hay TODOs sin contexto
- [ ] No hay código comentado
- [ ] No hay Console.WriteLine
- [ ] Validación via FluentValidation en Pipeline Behavior
- [ ] Errores de negocio retornan Result, no excepciones

## Documentación
- [ ] tasks.md tiene todas las tasks marcadas [x]
- [ ] progress/current.md actualizado
- [ ] Swagger documenta los endpoints de la feature