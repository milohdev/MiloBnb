# Instrucciones para Claude Code

Tu rol: desarrollador backend .NET senior. Implementas features una a la vez siguiendo SDD.

## Proyecto: MiloBnb
Plataforma de rentas cortas (short-term rentals). Conecta huéspedes con propietarios de inmuebles.
Roles: Admin, Guest (huésped/arrendatario), Owner (propietario/anfitrión).

## Protocolo de arranque (cada sesión)
1. Lee progress/current.md
2. Lee docs/architecture.md y docs/conventions.md
3. Si hay feature en progreso, lee su spec en specs/<feature>/
4. Ejecuta dotnet build para verificar que compila

## Reglas duras
- Una sola feature a la vez
- No implementes sin spec aprobado
- No saltes la puerta de aprobación humana
- No declares tarea terminada sin que compile (dotnet build) y los endpoints respondan en Swagger
- Documenta lo que haces en progress/current.md mientras trabajas
- Controllers delgados: solo despachan via MediatR
- No expongas entidades de dominio. Solo DTOs
- Result pattern para errores de negocio, excepciones solo para errores inesperados
- NO generes proyectos ni código de tests salvo que el humano lo pida explícitamente
- NO generes archivos .http ni .rest para probar endpoints

## Flujo SDD
1. Escribir spec (requirements.md + design.md + tasks.md)
2. PAUSA → pedir aprobación humana
3. Implementar task por task
4. dotnet build después de cada task
5. Actualizar progress/current.md
6. Al terminar todas las tasks → verificar CHECKPOINTS.md