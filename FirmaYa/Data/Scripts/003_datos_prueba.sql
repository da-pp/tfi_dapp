-- FirmaYA - 003: datos de prueba. Solo para bases de desarrollo/prueba, nunca en producción.
-- Contraseña de todos los usuarios demo: FirmaYa2026!
-- Los hashes tienen el formato de ASP.NET Core PasswordHasher (V3, PBKDF2-HMAC-SHA512, 100.000 iteraciones).

BEGIN;

-- Roles internos (CU-15 paso 8, CU-19).
INSERT INTO rol (nombre, descripcion) VALUES
    ('Administrador', 'Gestiona usuarios, plantillas y BackOffice'),
    ('Abogado', 'Crea y gestiona contratos'),
    ('Agente Inmobiliario', 'Crea y gestiona contratos');

-- Estados del contrato (CU-05, CU-17 paso 13).
INSERT INTO estado_contrato (nombre) VALUES
    ('Borrador'),
    ('En Revisión'),
    ('Listo para firmar'),
    ('Firmado'),
    ('Archivado');

-- Transiciones válidas (CU-05 paso 4).
INSERT INTO transicion_estado (id_estado_origen, id_estado_destino)
SELECT o.id_estado, d.id_estado
FROM (VALUES ('Borrador', 'En Revisión'),
             ('En Revisión', 'Listo para firmar'),
             ('Firmado', 'Archivado')) AS t(origen, destino)
JOIN estado_contrato o ON o.nombre = t.origen
JOIN estado_contrato d ON d.nombre = t.destino;

-- Usuarios demo (nombres tomados de los prototipos del documento).
INSERT INTO usuario (id_rol, nombre, apellido, email, contrasena_hash, estado, intentos_fallidos)
SELECT r.id_rol, u.nombre, u.apellido, u.email, u.hash, u.estado, 0
FROM (VALUES
    ('Administrador', 'Daniel', 'Pérez', 'daniel@mail.com',
     'AQAAAAIAAYagAAAAEOjL2RP8ihxGlE3Wwi1NmfPk2l3d8Yn6tewrxdfHE+8zGAjS5s0nQfKnzc3LApSmsQ==', 'Activo'),
    ('Abogado', 'María', 'Gómez', 'maria@mail.com',
     'AQAAAAIAAYagAAAAEA4IBxW7RwAkDg8X4H5rc8SRY+XnZmKNq5wMoZUQ5YVbPiNpXVtbf/DNpnzPHZYxJQ==', 'Activo'),
    ('Agente Inmobiliario', 'Carlos', 'Ruiz', 'carlos@mail.com',
     'AQAAAAIAAYagAAAAEByfrqqFfFDbHO2uPsNloHlwyN4Sj2y+7KJKo1RSsFUOXY0qV6vkWkuMUgfb4mLWSA==', 'Activo'),
    ('Agente Inmobiliario', 'Laura', 'Díaz', 'laura@mail.com',
     'AQAAAAIAAYagAAAAEMH0LEmDVAIaKA6IVQFXHquyRWFhjFUaNPL9AIGYNwImrJysa32Qb4o1Gnn2UeS6XQ==', 'Inactivo')
) AS u(rol, nombre, apellido, email, hash, estado)
JOIN rol r ON r.nombre = u.rol;

-- Plantillas activas (CU-01 precondición: al menos una plantilla activa; CU-16 no forma parte de esta etapa).
-- Mismo contenido que PlantillasDemo en Data/Memory/DALMemoria.cs. Marcadores {{campo}} del CU-16.
INSERT INTO plantilla (nombre, tipo_contrato, descripcion_uso, cuerpo, estado, version) VALUES
    ('Locación vivienda', 'Arrendamiento', 'Locación de inmuebles destinados a vivienda.',
     E'CONTRATO DE LOCACIÓN\n\nEntre {{partes_involucradas}}, en adelante "LAS PARTES", acuerdan celebrar el presente contrato de locación denominado "{{nombre_contrato}}".\n\nPRIMERA - OBJETO. El inmueble objeto del presente se describe como: {{descripcion_propiedad}}.\n\nSEGUNDA - PLAZO. La locación comienza el {{fecha_inicio}} y finaliza el {{fecha_expiracion}}.\n\nTERCERA - FIRMA. Las partes dejan constancia de que el presente documento será firmado mediante mecanismo de verificación OTP y registro de integridad por hash.',
     'Activa', 1),
    ('Compraventa inmueble', 'Venta', 'Boleto de compraventa de inmuebles.',
     E'BOLETO DE COMPRAVENTA\n\nEntre {{partes_involucradas}}, en adelante "LAS PARTES", acuerdan celebrar el presente boleto de compraventa denominado "{{nombre_contrato}}".\n\nPRIMERA - OBJETO. El inmueble objeto de la operación se describe como: {{descripcion_propiedad}}.\n\nSEGUNDA - VIGENCIA. El boleto rige desde el {{fecha_inicio}} hasta el {{fecha_expiracion}}.\n\nTERCERA - FIRMA. Las partes dejan constancia de que el presente documento será firmado mediante mecanismo de verificación OTP y registro de integridad por hash.',
     'Activa', 1),
    ('Mandato administración', 'Mandato', 'Mandato para la administración de propiedades.',
     E'CONTRATO DE MANDATO\n\nEntre {{partes_involucradas}}, en adelante "LAS PARTES", acuerdan celebrar el presente contrato de mandato denominado "{{nombre_contrato}}".\n\nPRIMERA - OBJETO. El mandato comprende la administración de: {{descripcion_propiedad}}.\n\nSEGUNDA - PLAZO. El mandato rige desde el {{fecha_inicio}} hasta el {{fecha_expiracion}}.\n\nTERCERA - FIRMA. Las partes dejan constancia de que el presente documento será firmado mediante mecanismo de verificación OTP y registro de integridad por hash.',
     'Activa', 1),
    ('Contrato general', 'Otro', 'Modelo general para otros acuerdos.',
     E'CONTRATO\n\nEntre {{partes_involucradas}}, en adelante "LAS PARTES", acuerdan celebrar el presente contrato denominado "{{nombre_contrato}}".\n\nPRIMERA - OBJETO. {{descripcion_propiedad}}.\n\nSEGUNDA - PLAZO. El contrato rige desde el {{fecha_inicio}} hasta el {{fecha_expiracion}}.\n\nTERCERA - FIRMA. Las partes dejan constancia de que el presente documento será firmado mediante mecanismo de verificación OTP y registro de integridad por hash.',
     'Activa', 1);

-- Campos dinámicos detectados en cada plantilla (CU-16 paso 14).
INSERT INTO campo_plantilla (id_plantilla, marcador, tipo_dato)
SELECT p.id_plantilla, c.marcador, c.tipo_dato
FROM plantilla p
CROSS JOIN (VALUES ('{{nombre_contrato}}', 'texto'),
                   ('{{partes_involucradas}}', 'texto'),
                   ('{{descripcion_propiedad}}', 'texto'),
                   ('{{fecha_inicio}}', 'fecha'),
                   ('{{fecha_expiracion}}', 'fecha')) AS c(marcador, tipo_dato);

COMMIT;
