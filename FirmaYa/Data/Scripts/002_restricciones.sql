-- FirmaYA - 002: claves foráneas y restricciones documentadas.
-- Ejecutar después de 001_crear_esquema.sql.

BEGIN;

ALTER TABLE usuario
    ADD CONSTRAINT fk_usuario_id_rol FOREIGN KEY (id_rol) REFERENCES rol (id_rol),
    -- CU-15 paso 13: el email no puede estar ya registrado en el sistema.
    ADD CONSTRAINT uq_usuario_email UNIQUE (email);

ALTER TABLE sesion
    ADD CONSTRAINT fk_sesion_id_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario);

ALTER TABLE token_seguridad
    ADD CONSTRAINT fk_token_seguridad_id_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario);

ALTER TABLE preferencia_notificacion
    ADD CONSTRAINT fk_preferencia_notificacion_id_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario);

ALTER TABLE auditoria
    ADD CONSTRAINT fk_auditoria_id_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario);

ALTER TABLE transicion_estado
    ADD CONSTRAINT fk_transicion_estado_id_estado_origen FOREIGN KEY (id_estado_origen) REFERENCES estado_contrato (id_estado),
    ADD CONSTRAINT fk_transicion_estado_id_estado_destino FOREIGN KEY (id_estado_destino) REFERENCES estado_contrato (id_estado);

ALTER TABLE campo_plantilla
    ADD CONSTRAINT fk_campo_plantilla_id_plantilla FOREIGN KEY (id_plantilla) REFERENCES plantilla (id_plantilla);

ALTER TABLE contrato
    ADD CONSTRAINT fk_contrato_id_usuario_creador FOREIGN KEY (id_usuario_creador) REFERENCES usuario (id_usuario),
    ADD CONSTRAINT fk_contrato_id_estado FOREIGN KEY (id_estado) REFERENCES estado_contrato (id_estado),
    ADD CONSTRAINT fk_contrato_id_plantilla FOREIGN KEY (id_plantilla) REFERENCES plantilla (id_plantilla);

ALTER TABLE usuario_contrato
    ADD CONSTRAINT fk_usuario_contrato_id_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
    ADD CONSTRAINT fk_usuario_contrato_id_contrato FOREIGN KEY (id_contrato) REFERENCES contrato (id_contrato);

ALTER TABLE comentario
    ADD CONSTRAINT fk_comentario_id_contrato FOREIGN KEY (id_contrato) REFERENCES contrato (id_contrato),
    ADD CONSTRAINT fk_comentario_id_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario);

ALTER TABLE notificacion
    ADD CONSTRAINT fk_notificacion_id_usuario FOREIGN KEY (id_usuario) REFERENCES usuario (id_usuario),
    ADD CONSTRAINT fk_notificacion_id_contrato FOREIGN KEY (id_contrato) REFERENCES contrato (id_contrato);

ALTER TABLE version_contrato
    ADD CONSTRAINT fk_version_contrato_id_contrato FOREIGN KEY (id_contrato) REFERENCES contrato (id_contrato),
    ADD CONSTRAINT fk_version_contrato_id_usuario_autor FOREIGN KEY (id_usuario_autor) REFERENCES usuario (id_usuario);

ALTER TABLE firma
    ADD CONSTRAINT fk_firma_id_version FOREIGN KEY (id_version) REFERENCES version_contrato (id_version),
    ADD CONSTRAINT fk_firma_id_parte FOREIGN KEY (id_parte) REFERENCES usuario_contrato (id_parte);

ALTER TABLE documento_pdf
    ADD CONSTRAINT fk_documento_pdf_id_version FOREIGN KEY (id_version) REFERENCES version_contrato (id_version);

-- Supabase expone el esquema public por su API REST. FirmaYa no la usa:
-- se habilita RLS sin políticas para que los roles anon/authenticated no accedan.
-- La aplicación se conecta como usuario de base de datos y no se ve afectada.
ALTER TABLE rol ENABLE ROW LEVEL SECURITY;
ALTER TABLE usuario ENABLE ROW LEVEL SECURITY;
ALTER TABLE sesion ENABLE ROW LEVEL SECURITY;
ALTER TABLE token_seguridad ENABLE ROW LEVEL SECURITY;
ALTER TABLE preferencia_notificacion ENABLE ROW LEVEL SECURITY;
ALTER TABLE auditoria ENABLE ROW LEVEL SECURITY;
ALTER TABLE estado_contrato ENABLE ROW LEVEL SECURITY;
ALTER TABLE transicion_estado ENABLE ROW LEVEL SECURITY;
ALTER TABLE plantilla ENABLE ROW LEVEL SECURITY;
ALTER TABLE campo_plantilla ENABLE ROW LEVEL SECURITY;
ALTER TABLE contrato ENABLE ROW LEVEL SECURITY;
ALTER TABLE usuario_contrato ENABLE ROW LEVEL SECURITY;
ALTER TABLE comentario ENABLE ROW LEVEL SECURITY;
ALTER TABLE notificacion ENABLE ROW LEVEL SECURITY;
ALTER TABLE version_contrato ENABLE ROW LEVEL SECURITY;
ALTER TABLE firma ENABLE ROW LEVEL SECURITY;
ALTER TABLE documento_pdf ENABLE ROW LEVEL SECURITY;

COMMIT;
