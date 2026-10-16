/* =====================================================================
   Clínica Villa Salud - Plataforma de Gestión de Citas Médicas
   02_migracion_desde_v1.sql : actualiza una base BD_CitasMedicas existente
   (8 tablas: ROL, USUARIO, PACIENTE, ESPECIALIDAD, MEDICO, CONSULTORIO,
   ESTADO_CITA, CITA) al modelo actual de 15 tablas CONSERVANDO los datos.

   ANTES DE EJECUTAR
     1. Haga una copia de seguridad:
          BACKUP DATABASE BD_CitasMedicas TO DISK = 'C:\Respaldo\BD_CitasMedicas.bak';
     2. Ejecute el script completo de una sola vez. Todo corre en una transacción:
        si un paso falla se revierte todo y los pasos siguientes no se ejecutan
        (cada bloque verifica @@TRANCOUNT), de modo que la base queda como estaba.

   QUÉ CAMBIA
     - Contraseñas: dejan de guardarse en texto. Como un hash PBKDF2 no puede
       calcularse en T-SQL, todos los usuarios quedan con la clave temporal
       Temporal#2026 y el sistema les obliga a cambiarla en su primer ingreso.
     - Roles: se normalizan a Administrador, Medico y Recepcionista.
     - Estados de cita: se normalizan a los cinco estados fijos
       (1 Pendiente, 2 Reprogramada, 3 Atendida, 4 Cancelada, 5 No asistió).
     - Sedes: se crean las dos sedes; los consultorios y citas existentes se
       asignan a la sede Villa El Salvador (se puede corregir luego desde el sistema).
     - Citas duplicadas (mismo médico o consultorio, misma fecha y hora): se
       conserva la más antigua y las demás pasan a Cancelada con una observación.
     - Los usuarios con rol Medico quedan sin médico asociado: el administrador
       debe vincularlos desde Administración > Usuarios.
     - Los pacientes existentes quedan SIN consentimiento registrado: recepción
       debe recabarlo antes de programarles una nueva cita (Ley N.° 29733).
   ===================================================================== */
USE BD_CitasMedicas;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('SEDE') IS NOT NULL OR COL_LENGTH('USUARIO', 'password_hash') IS NOT NULL
BEGIN
    RAISERROR('Esta base ya fue migrada. No se realizó ningún cambio.', 16, 1);
    SET NOEXEC ON;
END
GO
IF OBJECT_ID('CITA') IS NULL OR COL_LENGTH('USUARIO', 'password') IS NULL
BEGIN
    RAISERROR('No se encontró el modelo original (tablas CITA y USUARIO.password). No se realizó ningún cambio.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;
GO

/* ---------- 1. Sedes ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE TABLE SEDE (
    id_sede   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SEDE PRIMARY KEY,
    nombre    VARCHAR(80)  NOT NULL CONSTRAINT UQ_SEDE_NOMBRE UNIQUE,
    direccion VARCHAR(150) NOT NULL,
    distrito  VARCHAR(60)  NOT NULL,
    estado    BIT NOT NULL CONSTRAINT DF_SEDE_ESTADO DEFAULT 1
);
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
INSERT INTO SEDE (nombre, direccion, distrito) VALUES
    ('Villa El Salvador',      'Av. Revolución 1250', 'Villa El Salvador'),
    ('San Juan de Miraflores', 'Av. San Juan 980',    'San Juan de Miraflores');
GO

/* ---------- 2. Roles: tres roles fijos ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
-- Clasificación de los roles existentes según su nombre
SELECT id_rol,
       CASE WHEN nombre LIKE '%admin%' THEN 'Administrador'
            WHEN nombre LIKE '%m[eé]dic%' OR nombre LIKE '%doctor%' THEN 'Medico'
            ELSE 'Recepcionista' END AS destino
INTO #rol_mapa
FROM ROL;

-- Se liberan los nombres actuales para poder insertar los definitivos
UPDATE ROL SET nombre = LEFT('~' + CAST(id_rol AS VARCHAR(10)) + '_' + nombre, 50);
INSERT INTO ROL (nombre) VALUES ('Administrador'), ('Medico'), ('Recepcionista');

UPDATE u SET id_rol = nuevo.id_rol
FROM USUARIO u
JOIN #rol_mapa m ON m.id_rol = u.id_rol
JOIN ROL nuevo   ON nuevo.nombre = m.destino;

DELETE FROM ROL WHERE nombre LIKE '~%';
DROP TABLE #rol_mapa;
GO

/* ---------- 3. Usuarios: hash de contraseña, sede, médico y bloqueo ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
ALTER TABLE USUARIO ADD
    password_hash      VARCHAR(200) NULL,
    id_sede            INT NULL,
    id_medico          INT NULL,
    intentos_fallidos  INT NOT NULL CONSTRAINT DF_USUARIO_INTENTOS DEFAULT 0,
    bloqueado_hasta    DATETIME2(0) NULL,
    debe_cambiar_clave BIT NOT NULL CONSTRAINT DF_USUARIO_CAMBIO DEFAULT 0;
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
-- Clave temporal Temporal#2026 (hash PBKDF2 generado por la aplicación) y cambio obligatorio
UPDATE USUARIO
SET password_hash = 'AQAAAAIAAYagAAAAEHnRV4tnsy69MnQkQT32sIwau3eViL8dXPD8ahJpponmXAI88lX1JYnI378/ZHp/CA==',
    debe_cambiar_clave = 1;

-- Recepcionistas y médicos pertenecen a una sede; el administrador ve todas
UPDATE u SET id_sede = 1
FROM USUARIO u JOIN ROL r ON r.id_rol = u.id_rol
WHERE r.nombre <> 'Administrador';
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
ALTER TABLE USUARIO ALTER COLUMN password_hash VARCHAR(200) NOT NULL;
ALTER TABLE USUARIO DROP COLUMN password;      -- se elimina la contraseña en texto
ALTER TABLE USUARIO ADD
    CONSTRAINT FK_USUARIO_SEDE   FOREIGN KEY (id_sede)   REFERENCES SEDE(id_sede),
    CONSTRAINT FK_USUARIO_MEDICO FOREIGN KEY (id_medico) REFERENCES MEDICO(id_medico);
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
-- Si no quedó ningún administrador activo, se crea uno para no perder el acceso
IF NOT EXISTS (SELECT 1 FROM USUARIO u JOIN ROL r ON r.id_rol = u.id_rol WHERE r.nombre = 'Administrador' AND u.estado = 1)
   AND NOT EXISTS (SELECT 1 FROM USUARIO WHERE username = 'admin')
    INSERT INTO USUARIO (username, password_hash, id_rol, debe_cambiar_clave, estado)
    SELECT 'admin', 'AQAAAAIAAYagAAAAEHnRV4tnsy69MnQkQT32sIwau3eViL8dXPD8ahJpponmXAI88lX1JYnI378/ZHp/CA==', id_rol, 1, 1
    FROM ROL WHERE nombre = 'Administrador';
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE TABLE REFRESH_TOKEN (
    id_token   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_REFRESH_TOKEN PRIMARY KEY,
    id_usuario INT NOT NULL,
    token_hash VARCHAR(64) NOT NULL CONSTRAINT UQ_REFRESH_TOKEN_HASH UNIQUE,
    expira     DATETIME2(0) NOT NULL,
    revocado   BIT NOT NULL CONSTRAINT DF_REFRESH_REVOCADO DEFAULT 0,
    CONSTRAINT FK_REFRESH_USUARIO FOREIGN KEY (id_usuario) REFERENCES USUARIO(id_usuario) ON DELETE CASCADE
);
CREATE INDEX IX_REFRESH_TOKEN_USUARIO ON REFRESH_TOKEN(id_usuario);
GO

/* ---------- 4. Correo opcional en médicos y pacientes ----------
   El modelo original declaraba email UNIQUE: SQL Server solo permite un NULL en
   una restricción UNIQUE, por lo que no se podía registrar a una segunda persona
   sin correo. Se quita esa restricción (su nombre es generado, se busca en el catálogo). */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_NAME(kc.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(kc.name) + N';'
FROM sys.key_constraints kc
JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
JOIN sys.columns c        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE kc.type = 'UQ' AND c.name = 'email' AND OBJECT_NAME(kc.parent_object_id) IN ('MEDICO', 'PACIENTE');
EXEC sp_executesql @sql;
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE UNIQUE INDEX UX_MEDICO_EMAIL ON MEDICO(email) WHERE email IS NOT NULL;
CREATE INDEX IX_PACIENTE_APELLIDOS ON PACIENTE(apellidos, nombres);
GO

/* ---------- 5. Consultorios por sede ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
ALTER TABLE CONSULTORIO ADD id_sede INT NULL;
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
UPDATE CONSULTORIO SET id_sede = 1;
-- Números repetidos en la misma sede: se distinguen con el identificador
UPDATE c SET numero = LEFT(c.numero, 5) + '-' + CAST(c.id_consultorio AS VARCHAR(4))
FROM CONSULTORIO c
WHERE EXISTS (SELECT 1 FROM CONSULTORIO o WHERE o.numero = c.numero AND o.id_consultorio < c.id_consultorio);
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
ALTER TABLE CONSULTORIO ALTER COLUMN id_sede INT NOT NULL;
ALTER TABLE CONSULTORIO ADD
    CONSTRAINT FK_CONSULTORIO_SEDE FOREIGN KEY (id_sede) REFERENCES SEDE(id_sede),
    CONSTRAINT UQ_CONSULTORIO_SEDE_NUMERO UNIQUE (id_sede, numero);
GO

/* ---------- 6. Horarios de atención ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE TABLE HORARIO_MEDICO (
    id_horario        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HORARIO_MEDICO PRIMARY KEY,
    id_medico         INT NOT NULL,
    id_consultorio    INT NOT NULL,
    dia_semana        TINYINT NOT NULL,
    hora_inicio       TIME(0) NOT NULL,
    hora_fin          TIME(0) NOT NULL,
    duracion_cita_min TINYINT NOT NULL CONSTRAINT DF_HORARIO_DURACION DEFAULT 30,
    vigente           BIT NOT NULL CONSTRAINT DF_HORARIO_VIGENTE DEFAULT 1,
    CONSTRAINT FK_HORARIO_MEDICO      FOREIGN KEY (id_medico)      REFERENCES MEDICO(id_medico),
    CONSTRAINT FK_HORARIO_CONSULTORIO FOREIGN KEY (id_consultorio) REFERENCES CONSULTORIO(id_consultorio),
    CONSTRAINT CHK_HORARIO_DIA      CHECK (dia_semana BETWEEN 0 AND 6),
    CONSTRAINT CHK_HORARIO_HORAS    CHECK (hora_inicio < hora_fin),
    CONSTRAINT CHK_HORARIO_DURACION CHECK (duracion_cita_min BETWEEN 10 AND 120)
);
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE INDEX IX_HORARIO_MEDICO_DIA ON HORARIO_MEDICO(id_medico, dia_semana) WHERE vigente = 1;
GO

/* ---------- 7. Consentimiento informado ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE TABLE CONSENTIMIENTO (
    id_consentimiento INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CONSENTIMIENTO PRIMARY KEY,
    id_paciente       INT NOT NULL,
    finalidad         VARCHAR(200) NOT NULL,
    version_texto     VARCHAR(10)  NOT NULL,
    fecha_aceptacion  DATETIME2(0) NOT NULL,
    canal             VARCHAR(20)  NOT NULL,
    revocado          BIT NOT NULL CONSTRAINT DF_CONSENTIMIENTO_REVOCADO DEFAULT 0,
    CONSTRAINT FK_CONSENTIMIENTO_PACIENTE FOREIGN KEY (id_paciente) REFERENCES PACIENTE(id_paciente) ON DELETE CASCADE
);
CREATE INDEX IX_CONSENTIMIENTO_PACIENTE ON CONSENTIMIENTO(id_paciente);
GO

/* ---------- 8. Estados de cita: catálogo fijo ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE TABLE ESTADO_CITA_NUEVO (
    id_estado_cita INT NOT NULL CONSTRAINT PK_ESTADO_CITA_V2 PRIMARY KEY,
    nombre         VARCHAR(100) NOT NULL CONSTRAINT UQ_ESTADO_CITA_NOMBRE UNIQUE
);
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
INSERT INTO ESTADO_CITA_NUEVO (id_estado_cita, nombre) VALUES
    (1, 'Pendiente'), (2, 'Reprogramada'), (3, 'Atendida'), (4, 'Cancelada'), (5, 'No asistió');

ALTER TABLE CITA ADD id_estado_nuevo INT NULL;
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
UPDATE c SET id_estado_nuevo =
       CASE WHEN e.nombre LIKE '%cancel%' OR e.nombre LIKE '%anul%' THEN 4
            WHEN e.nombre LIKE '%no asist%' OR e.nombre LIKE '%inasist%' OR e.nombre LIKE '%ausen%' THEN 5
            WHEN e.nombre LIKE '%reprog%' THEN 2
            WHEN e.nombre LIKE '%atend%' OR e.nombre LIKE '%complet%' OR e.nombre LIKE '%finaliz%' OR e.nombre LIKE '%realiz%' THEN 3
            ELSE 1 END
FROM CITA c JOIN ESTADO_CITA e ON e.id_estado_cita = c.id_estado_cita;
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
ALTER TABLE CITA DROP CONSTRAINT FK_CITA_ESTADO;
ALTER TABLE CITA DROP COLUMN id_estado_cita;
DROP TABLE ESTADO_CITA;
EXEC sp_rename 'ESTADO_CITA_NUEVO', 'ESTADO_CITA';
EXEC sp_rename 'PK_ESTADO_CITA_V2', 'PK_ESTADO_CITA', 'OBJECT';
EXEC sp_rename 'CITA.id_estado_nuevo', 'id_estado_cita', 'COLUMN';
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
ALTER TABLE CITA ALTER COLUMN id_estado_cita INT NOT NULL;
ALTER TABLE CITA ADD
    CONSTRAINT DF_CITA_ESTADO DEFAULT 1 FOR id_estado_cita,
    CONSTRAINT FK_CITA_ESTADO FOREIGN KEY (id_estado_cita) REFERENCES ESTADO_CITA(id_estado_cita);
GO

/* ---------- 9. Citas: código, sede, usuario que registra y concurrencia ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
ALTER TABLE CITA ADD
    codigo              VARCHAR(12) NULL,
    id_sede             INT NULL,
    id_usuario_registro INT NULL,
    row_version         ROWVERSION NOT NULL;
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
-- Código de las citas anteriores: M + número de cita (las nuevas usan C + fecha + sufijo)
UPDATE CITA SET codigo = 'M' + RIGHT('00000000000' + CAST(id_cita AS VARCHAR(11)), 11);
UPDATE c SET id_sede = k.id_sede FROM CITA c JOIN CONSULTORIO k ON k.id_consultorio = c.id_consultorio;
UPDATE CITA SET fecha_registro = GETDATE() WHERE fecha_registro IS NULL;

-- Cruces heredados: se conserva la cita más antigua de cada médico/consultorio y hora
;WITH repetidas AS (
    SELECT id_cita,
           ROW_NUMBER() OVER (PARTITION BY id_medico, fecha, hora_inicio ORDER BY id_cita)      AS n_medico,
           ROW_NUMBER() OVER (PARTITION BY id_consultorio, fecha, hora_inicio ORDER BY id_cita) AS n_consultorio
    FROM CITA WHERE id_estado_cita <> 4
)
UPDATE c SET id_estado_cita = 4,
             observacion = LEFT('Cancelada en la migración por cruce de horario. ' + ISNULL(c.observacion, ''), 250)
FROM CITA c JOIN repetidas r ON r.id_cita = c.id_cita
WHERE r.n_medico > 1 OR r.n_consultorio > 1;
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
-- hora_inicio / hora_fin pasan a precisión de segundos (igual que el modelo de la aplicación)
ALTER TABLE CITA DROP CONSTRAINT CHK_HORAS;
ALTER TABLE CITA ALTER COLUMN hora_inicio TIME(0) NOT NULL;
ALTER TABLE CITA ALTER COLUMN hora_fin    TIME(0) NOT NULL;
ALTER TABLE CITA ALTER COLUMN codigo      VARCHAR(12) NOT NULL;
ALTER TABLE CITA ALTER COLUMN id_sede     INT NOT NULL;
ALTER TABLE CITA ALTER COLUMN fecha_registro DATETIME NOT NULL;
ALTER TABLE CITA ADD
    CONSTRAINT CHK_HORAS       CHECK (hora_inicio < hora_fin),
    CONSTRAINT UQ_CITA_CODIGO  UNIQUE (codigo),
    CONSTRAINT FK_CITA_SEDE    FOREIGN KEY (id_sede)             REFERENCES SEDE(id_sede),
    CONSTRAINT FK_CITA_USUARIO FOREIGN KEY (id_usuario_registro) REFERENCES USUARIO(id_usuario);
GO
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE UNIQUE INDEX UX_CITA_MEDICO_HORA      ON CITA(id_medico, fecha, hora_inicio)      WHERE id_estado_cita <> 4;
CREATE UNIQUE INDEX UX_CITA_CONSULTORIO_HORA ON CITA(id_consultorio, fecha, hora_inicio) WHERE id_estado_cita <> 4;
CREATE INDEX IX_CITA_FECHA_SEDE ON CITA(fecha, id_sede) INCLUDE (id_medico, id_estado_cita);
CREATE INDEX IX_CITA_PACIENTE   ON CITA(id_paciente, fecha);
GO

/* ---------- 10. Historial, notificaciones y auditoría ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
CREATE TABLE HISTORIAL_CITA (
    id_historial       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HISTORIAL_CITA PRIMARY KEY,
    id_cita            INT NOT NULL,
    id_estado_anterior INT NULL,
    id_estado_nuevo    INT NOT NULL,
    motivo             VARCHAR(250) NULL,
    id_usuario         INT NULL,
    fecha              DATETIME2(0) NOT NULL,
    CONSTRAINT FK_HISTORIAL_CITA     FOREIGN KEY (id_cita)            REFERENCES CITA(id_cita) ON DELETE CASCADE,
    CONSTRAINT FK_HISTORIAL_ANTERIOR FOREIGN KEY (id_estado_anterior) REFERENCES ESTADO_CITA(id_estado_cita),
    CONSTRAINT FK_HISTORIAL_NUEVO    FOREIGN KEY (id_estado_nuevo)    REFERENCES ESTADO_CITA(id_estado_cita),
    CONSTRAINT FK_HISTORIAL_USUARIO  FOREIGN KEY (id_usuario)         REFERENCES USUARIO(id_usuario)
);
CREATE INDEX IX_HISTORIAL_CITA ON HISTORIAL_CITA(id_cita, fecha);

-- Punto de partida del historial para las citas existentes
INSERT INTO HISTORIAL_CITA (id_cita, id_estado_anterior, id_estado_nuevo, motivo, id_usuario, fecha)
SELECT id_cita, NULL, id_estado_cita, 'Estado al migrar la base de datos', NULL, SYSDATETIME() FROM CITA;

CREATE TABLE NOTIFICACION (
    id_notificacion  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NOTIFICACION PRIMARY KEY,
    id_cita          INT NOT NULL,
    tipo             VARCHAR(20)  NOT NULL,
    destino          VARCHAR(100) NOT NULL,
    estado_envio     VARCHAR(20)  NOT NULL,
    fecha_programada DATETIME2(0) NOT NULL,
    fecha_envio      DATETIME2(0) NULL,
    detalle          VARCHAR(250) NULL,
    CONSTRAINT FK_NOTIFICACION_CITA FOREIGN KEY (id_cita) REFERENCES CITA(id_cita) ON DELETE CASCADE
);
CREATE INDEX IX_NOTIFICACION_PENDIENTE ON NOTIFICACION(estado_envio, fecha_programada);

CREATE TABLE AUDITORIA (
    id_auditoria BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AUDITORIA PRIMARY KEY,
    id_usuario   INT NULL,
    accion       VARCHAR(40) NOT NULL,
    entidad      VARCHAR(40) NOT NULL,
    id_entidad   VARCHAR(40) NULL,
    valores_json NVARCHAR(MAX) NULL,
    ip           VARCHAR(45) NULL,
    fecha        DATETIME2(0) NOT NULL,
    CONSTRAINT FK_AUDITORIA_USUARIO FOREIGN KEY (id_usuario) REFERENCES USUARIO(id_usuario)
);
CREATE INDEX IX_AUDITORIA_FECHA ON AUDITORIA(fecha DESC);

INSERT INTO AUDITORIA (id_usuario, accion, entidad, id_entidad, valores_json, ip, fecha)
VALUES (NULL, 'MIGRAR', 'BASE_DATOS', NULL, N'{"script":"02_migracion_desde_v1.sql"}', NULL, SYSDATETIME());
GO

/* ---------- 11. Reporte de productividad ---------- */
IF @@TRANCOUNT = 0 SET NOEXEC ON;
GO
CREATE PROCEDURE usp_ReporteProductividad
    @desde   DATE,
    @hasta   DATE,
    @id_sede INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.nombre AS sede,
           m.apellidos + ', ' + m.nombres AS medico,
           e.nombre AS especialidad,
           COUNT(*) AS total_citas,
           SUM(CASE WHEN c.id_estado_cita = 3 THEN 1 ELSE 0 END) AS atendidas,
           SUM(CASE WHEN c.id_estado_cita = 4 THEN 1 ELSE 0 END) AS canceladas,
           SUM(CASE WHEN c.id_estado_cita = 5 THEN 1 ELSE 0 END) AS no_asistio,
           SUM(CASE WHEN c.id_estado_cita IN (1, 2) THEN 1 ELSE 0 END) AS por_atender
    FROM CITA c
    JOIN SEDE s         ON s.id_sede = c.id_sede
    JOIN MEDICO m       ON m.id_medico = c.id_medico
    JOIN ESPECIALIDAD e ON e.id_especialidad = m.id_especialidad
    WHERE c.fecha BETWEEN @desde AND @hasta
      AND (@id_sede IS NULL OR c.id_sede = @id_sede)
    GROUP BY s.nombre, m.apellidos, m.nombres, e.nombre
    ORDER BY s.nombre, total_citas DESC;
END
GO

IF @@TRANCOUNT = 0 SET NOEXEC ON;
COMMIT TRANSACTION;
CREATE TABLE #migracion_ok (ok BIT);
GO
SET NOEXEC OFF;
GO
IF OBJECT_ID('tempdb..#migracion_ok') IS NOT NULL
BEGIN
    PRINT 'Migración terminada. Todos los usuarios ingresan con la clave temporal Temporal#2026 y deben cambiarla.';
    PRINT 'Pendiente en el sistema: registrar horarios de los médicos, vincular los usuarios médicos y recabar consentimientos.';
END
ELSE
    PRINT 'La migración NO se aplicó. Revise el primer mensaje de error; la base quedó sin cambios.';
GO
