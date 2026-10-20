-- FirmaYA - 004: consultas de verificación (solo lectura).

-- 1. Deben existir las 17 tablas.
SELECT COUNT(*) AS cantidad_tablas
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('rol', 'usuario', 'sesion', 'token_seguridad', 'preferencia_notificacion',
                     'auditoria', 'estado_contrato', 'transicion_estado', 'plantilla', 'campo_plantilla',
                     'contrato', 'usuario_contrato', 'comentario', 'notificacion', 'version_contrato',
                     'firma', 'documento_pdf');

-- 2. Claves foráneas creadas.
SELECT tc.table_name, tc.constraint_name
FROM information_schema.table_constraints tc
WHERE tc.table_schema = 'public' AND tc.constraint_type = 'FOREIGN KEY'
ORDER BY tc.table_name, tc.constraint_name;

-- 3. CU-19: usuarios con su rol, estado e intentos fallidos.
SELECT u.id_usuario, u.email, r.nombre AS rol, u.estado, u.intentos_fallidos
FROM usuario u
JOIN rol r ON r.id_rol = u.id_rol
ORDER BY u.id_usuario;

-- 4. CU-19: últimos eventos de inicio de sesión y bloqueo.
SELECT a.fecha_hora, u.email, a.tipo_accion, a.direccion_ip, a.descripcion
FROM auditoria a
LEFT JOIN usuario u ON u.id_usuario = a.id_usuario
WHERE a.tipo_accion IN ('Inicio de sesión', 'Bloqueo de cuenta')
ORDER BY a.fecha_hora DESC
LIMIT 20;
