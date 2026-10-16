/* =====================================================================
   Clínica Villa Salud - Plataforma de Gestión de Citas Médicas
   01_creacion_bd.sql : crea la base de datos completa con datos iniciales.

   Uso: ejecutar en SQL Server Management Studio sobre una instancia SIN
        la base BD_CitasMedicas. Si ya tiene la base del proyecto con
        datos que desea conservar, NO ejecute este script: use
        02_migracion_desde_v1.sql.

   Usuarios iniciales (contraseña: Clave#2026):
        admin           Administrador
        recepcion.ves   Recepcionista - sede Villa El Salvador
        recepcion.sjm   Recepcionista - sede San Juan de Miraflores
        erojas          Médico (Dra. Elena Rojas Vega)
   ===================================================================== */
SET NOCOUNT ON;
GO
IF DB_ID('BD_CitasMedicas') IS NOT NULL
BEGIN
    RAISERROR('La base BD_CitasMedicas ya existe. Para conservar sus datos use 02_migracion_desde_v1.sql.', 16, 1);
    SET NOEXEC ON;
END
GO
CREATE DATABASE BD_CitasMedicas;
GO
USE BD_CitasMedicas;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =========================================
-- SEDE
-- =========================================
CREATE TABLE SEDE (
    id_sede   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SEDE PRIMARY KEY,
    nombre    VARCHAR(80)  NOT NULL CONSTRAINT UQ_SEDE_NOMBRE UNIQUE,
    direccion VARCHAR(150) NOT NULL,
    distrito  VARCHAR(60)  NOT NULL,
    estado    BIT NOT NULL CONSTRAINT DF_SEDE_ESTADO DEFAULT 1
);
GO

-- =========================================
-- ROL
-- =========================================
CREATE TABLE ROL (
    id_rol INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ROL PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL CONSTRAINT UQ_ROL_NOMBRE UNIQUE
);
GO

-- =========================================
-- ESPECIALIDAD
-- =========================================
CREATE TABLE ESPECIALIDAD (
    id_especialidad INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ESPECIALIDAD PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL CONSTRAINT UQ_ESPECIALIDAD_NOMBRE UNIQUE
);
GO

-- =========================================
-- MEDICO
-- =========================================
CREATE TABLE MEDICO (
    id_medico          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MEDICO PRIMARY KEY,
    codigo_colegiatura VARCHAR(50)  NOT NULL CONSTRAINT UQ_MEDICO_COLEGIATURA UNIQUE,
    nombres            VARCHAR(100) NOT NULL,
    apellidos          VARCHAR(100) NOT NULL,
    id_especialidad    INT NOT NULL,
    telefono           VARCHAR(20)  NULL,
    email              VARCHAR(100) NULL,
    estado             BIT NOT NULL CONSTRAINT DF_MEDICO_ESTADO DEFAULT 1,
    CONSTRAINT FK_MEDICO_ESPECIALIDAD FOREIGN KEY (id_especialidad) REFERENCES ESPECIALIDAD(id_especialidad)
);
GO
-- El correo es opcional: la unicidad solo aplica cuando se registra
CREATE UNIQUE INDEX UX_MEDICO_EMAIL ON MEDICO(email) WHERE email IS NOT NULL;
GO

-- =========================================
-- USUARIO  (la contraseña se guarda como hash PBKDF2, nunca en texto)
-- =========================================
CREATE TABLE USUARIO (
    id_usuario         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_USUARIO PRIMARY KEY,
    username           VARCHAR(30)  NOT NULL CONSTRAINT UQ_USUARIO_USERNAME UNIQUE,
    password_hash      VARCHAR(200) NOT NULL,
    id_rol             INT NOT NULL,
    id_sede            INT NULL,      -- NULL = todas las sedes (administrador)
    id_medico          INT NULL,      -- solo para usuarios con rol Medico
    intentos_fallidos  INT NOT NULL CONSTRAINT DF_USUARIO_INTENTOS DEFAULT 0,
    bloqueado_hasta    DATETIME2(0) NULL,
    debe_cambiar_clave BIT NOT NULL CONSTRAINT DF_USUARIO_CAMBIO DEFAULT 0,
    estado             BIT NOT NULL CONSTRAINT DF_USUARIO_ESTADO DEFAULT 1,
    CONSTRAINT FK_USUARIO_ROL    FOREIGN KEY (id_rol)    REFERENCES ROL(id_rol),
    CONSTRAINT FK_USUARIO_SEDE   FOREIGN KEY (id_sede)   REFERENCES SEDE(id_sede),
    CONSTRAINT FK_USUARIO_MEDICO FOREIGN KEY (id_medico) REFERENCES MEDICO(id_medico)
);
GO

-- =========================================
-- REFRESH_TOKEN  (se guarda el SHA-256 del token, no el token)
-- =========================================
CREATE TABLE REFRESH_TOKEN (
    id_token   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_REFRESH_TOKEN PRIMARY KEY,
    id_usuario INT NOT NULL,
    token_hash VARCHAR(64) NOT NULL CONSTRAINT UQ_REFRESH_TOKEN_HASH UNIQUE,
    expira     DATETIME2(0) NOT NULL,
    revocado   BIT NOT NULL CONSTRAINT DF_REFRESH_REVOCADO DEFAULT 0,
    CONSTRAINT FK_REFRESH_USUARIO FOREIGN KEY (id_usuario) REFERENCES USUARIO(id_usuario) ON DELETE CASCADE
);
GO
CREATE INDEX IX_REFRESH_TOKEN_USUARIO ON REFRESH_TOKEN(id_usuario);
GO

-- =========================================
-- CONSULTORIO
-- =========================================
CREATE TABLE CONSULTORIO (
    id_consultorio INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CONSULTORIO PRIMARY KEY,
    id_sede        INT NOT NULL,
    numero         VARCHAR(10) NOT NULL,
    piso           VARCHAR(10) NULL,
    estado         BIT NOT NULL CONSTRAINT DF_CONSULTORIO_ESTADO DEFAULT 1,
    CONSTRAINT FK_CONSULTORIO_SEDE FOREIGN KEY (id_sede) REFERENCES SEDE(id_sede),
    CONSTRAINT UQ_CONSULTORIO_SEDE_NUMERO UNIQUE (id_sede, numero)
);
GO

-- =========================================
-- HORARIO_MEDICO  (dia_semana: 0 = domingo ... 6 = sábado)
-- =========================================
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
CREATE INDEX IX_HORARIO_MEDICO_DIA ON HORARIO_MEDICO(id_medico, dia_semana) WHERE vigente = 1;
GO

-- =========================================
-- PACIENTE
-- =========================================
CREATE TABLE PACIENTE (
    id_paciente      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PACIENTE PRIMARY KEY,
    dni              VARCHAR(10)  NOT NULL CONSTRAINT UQ_PACIENTE_DNI UNIQUE,
    nombres          VARCHAR(100) NOT NULL,
    apellidos        VARCHAR(100) NOT NULL,
    fecha_nacimiento DATE NULL,
    telefono         VARCHAR(20)  NULL,
    email            VARCHAR(100) NULL,
    direccion        VARCHAR(200) NULL,
    tipo_sangre      VARCHAR(5)   NULL,
    alergias         VARCHAR(500) NULL
);
GO
CREATE INDEX IX_PACIENTE_APELLIDOS ON PACIENTE(apellidos, nombres);
GO

-- =========================================
-- CONSENTIMIENTO  (Ley N.° 29733: tratamiento de datos personales)
-- =========================================
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
GO
CREATE INDEX IX_CONSENTIMIENTO_PACIENTE ON CONSENTIMIENTO(id_paciente);
GO

-- =========================================
-- ESTADO_CITA  (catálogo fijo: los identificadores se usan en la aplicación)
-- =========================================
CREATE TABLE ESTADO_CITA (
    id_estado_cita INT NOT NULL CONSTRAINT PK_ESTADO_CITA PRIMARY KEY,
    nombre         VARCHAR(100) NOT NULL CONSTRAINT UQ_ESTADO_CITA_NOMBRE UNIQUE
);
GO

-- =========================================
-- CITA
-- =========================================
CREATE TABLE CITA (
    id_cita             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CITA PRIMARY KEY,
    codigo              VARCHAR(12) NOT NULL CONSTRAINT UQ_CITA_CODIGO UNIQUE,
    id_sede             INT NOT NULL,
    id_paciente         INT NOT NULL,
    id_medico           INT NOT NULL,
    id_consultorio      INT NOT NULL,
    fecha               DATE NOT NULL,
    hora_inicio         TIME(0) NOT NULL,
    hora_fin            TIME(0) NOT NULL,
    motivo_consulta     VARCHAR(500) NULL,
    id_estado_cita      INT NOT NULL CONSTRAINT DF_CITA_ESTADO DEFAULT 1,
    observacion         VARCHAR(250) NULL,
    fecha_registro      DATETIME NOT NULL CONSTRAINT DF_CITA_FECHA_REGISTRO DEFAULT GETDATE(),
    id_usuario_registro INT NULL,
    row_version         ROWVERSION NOT NULL,     -- control de concurrencia optimista
    CONSTRAINT FK_CITA_SEDE        FOREIGN KEY (id_sede)             REFERENCES SEDE(id_sede),
    CONSTRAINT FK_CITA_PACIENTE    FOREIGN KEY (id_paciente)         REFERENCES PACIENTE(id_paciente),
    CONSTRAINT FK_CITA_MEDICO      FOREIGN KEY (id_medico)           REFERENCES MEDICO(id_medico),
    CONSTRAINT FK_CITA_CONSULTORIO FOREIGN KEY (id_consultorio)      REFERENCES CONSULTORIO(id_consultorio),
    CONSTRAINT FK_CITA_ESTADO      FOREIGN KEY (id_estado_cita)      REFERENCES ESTADO_CITA(id_estado_cita),
    CONSTRAINT FK_CITA_USUARIO     FOREIGN KEY (id_usuario_registro) REFERENCES USUARIO(id_usuario),
    CONSTRAINT CHK_HORAS CHECK (hora_inicio < hora_fin)
);
GO
-- Última barrera contra el cruce de horarios: aunque dos usuarios confirmen a la vez,
-- la base de datos rechaza la segunda cita del mismo médico o consultorio en la misma hora.
-- Las citas canceladas (estado 4) liberan el intervalo.
CREATE UNIQUE INDEX UX_CITA_MEDICO_HORA      ON CITA(id_medico, fecha, hora_inicio)      WHERE id_estado_cita <> 4;
CREATE UNIQUE INDEX UX_CITA_CONSULTORIO_HORA ON CITA(id_consultorio, fecha, hora_inicio) WHERE id_estado_cita <> 4;
CREATE INDEX IX_CITA_FECHA_SEDE ON CITA(fecha, id_sede) INCLUDE (id_medico, id_estado_cita);
CREATE INDEX IX_CITA_PACIENTE   ON CITA(id_paciente, fecha);
GO

-- =========================================
-- HISTORIAL_CITA  (trazabilidad de cada cambio de estado)
-- =========================================
CREATE TABLE HISTORIAL_CITA (
    id_historial       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HISTORIAL_CITA PRIMARY KEY,
    id_cita            INT NOT NULL,
    id_estado_anterior INT NULL,
    id_estado_nuevo    INT NOT NULL,
    motivo             VARCHAR(250) NULL,
    id_usuario         INT NULL,
    fecha              DATETIME2(0) NOT NULL,
    CONSTRAINT FK_HISTORIAL_CITA      FOREIGN KEY (id_cita)            REFERENCES CITA(id_cita) ON DELETE CASCADE,
    CONSTRAINT FK_HISTORIAL_ANTERIOR  FOREIGN KEY (id_estado_anterior) REFERENCES ESTADO_CITA(id_estado_cita),
    CONSTRAINT FK_HISTORIAL_NUEVO     FOREIGN KEY (id_estado_nuevo)    REFERENCES ESTADO_CITA(id_estado_cita),
    CONSTRAINT FK_HISTORIAL_USUARIO   FOREIGN KEY (id_usuario)         REFERENCES USUARIO(id_usuario)
);
GO
CREATE INDEX IX_HISTORIAL_CITA ON HISTORIAL_CITA(id_cita, fecha);
GO

-- =========================================
-- NOTIFICACION  (cola de correos de confirmación y recordatorio)
-- =========================================
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
GO
CREATE INDEX IX_NOTIFICACION_PENDIENTE ON NOTIFICACION(estado_envio, fecha_programada);
GO

-- =========================================
-- AUDITORIA  (quién hizo qué y cuándo)
-- =========================================
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
GO
CREATE INDEX IX_AUDITORIA_FECHA ON AUDITORIA(fecha DESC);
GO

-- =========================================
-- Reporte de productividad (consulta directa para SSMS o Power BI)
-- =========================================
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

/* =====================================================================
   DATOS INICIALES
   ===================================================================== */
INSERT INTO ESTADO_CITA (id_estado_cita, nombre) VALUES
    (1, 'Pendiente'), (2, 'Reprogramada'), (3, 'Atendida'), (4, 'Cancelada'), (5, 'No asistió');

INSERT INTO ROL (nombre) VALUES ('Administrador'), ('Medico'), ('Recepcionista');   -- ids 1, 2, 3

INSERT INTO SEDE (nombre, direccion, distrito) VALUES
    ('Villa El Salvador',      'Av. Revolución 1250',       'Villa El Salvador'),
    ('San Juan de Miraflores', 'Av. San Juan 980',          'San Juan de Miraflores');

INSERT INTO ESPECIALIDAD (nombre) VALUES
    ('Cardiología'), ('Pediatría'), ('Medicina General'), ('Ginecología'), ('Traumatología');

INSERT INTO MEDICO (codigo_colegiatura, nombres, apellidos, id_especialidad, telefono, email) VALUES
    ('CMP-45120', 'Elena', 'Rojas Vega',   1, '987654321', 'erojas@villasalud.pe'),
    ('CMP-38877', 'Mario', 'Paz Huamán',   2, '987654322', 'mpaz@villasalud.pe'),
    ('CMP-51203', 'Lucía', 'Campos Ríos',  3, '987654323', 'lcampos@villasalud.pe'),
    ('CMP-60417', 'Jorge', 'Salas Medina', 4, '987654324', 'jsalas@villasalud.pe');

INSERT INTO CONSULTORIO (id_sede, numero, piso) VALUES
    (1, '101', '1'), (1, '102', '1'), (1, '201', '2'),
    (2, '101', '1'), (2, '102', '1');
GO

-- Horarios de lunes (1) a sábado (6)
INSERT INTO HORARIO_MEDICO (id_medico, id_consultorio, dia_semana, hora_inicio, hora_fin, duracion_cita_min)
SELECT h.id_medico, h.id_consultorio, d.dia, h.hora_inicio, h.hora_fin, h.duracion
FROM (VALUES (1), (2), (3), (4), (5), (6)) AS d(dia)
CROSS JOIN (VALUES
    (1, 1, '08:00', '13:00', 30),     -- Dra. Rojas    - VES 101
    (2, 2, '08:00', '12:00', 20),     -- Dr. Paz       - VES 102
    (3, 4, '14:00', '19:00', 30),     -- Dra. Campos   - SJM 101
    (4, 5, '09:00', '13:00', 30)      -- Dr. Salas     - SJM 102
) AS h(id_medico, id_consultorio, hora_inicio, hora_fin, duracion);
GO

-- Contraseña inicial de todos: Clave#2026 (hash PBKDF2-HMAC-SHA512, 100 000 iteraciones, generado por la aplicación)
DECLARE @hash VARCHAR(200) = 'AQAAAAIAAYagAAAAEPDli2f0RoDcZXo9hXooZ2WYgZscswvi3wGZXI5IoMfsFeQ+B6cDKvviMoJ5DAaA6g==';
INSERT INTO USUARIO (username, password_hash, id_rol, id_sede, id_medico) VALUES
    ('admin',         @hash, 1, NULL, NULL),
    ('recepcion.ves', @hash, 3, 1,    NULL),
    ('recepcion.sjm', @hash, 3, 2,    NULL),
    ('erojas',        @hash, 2, 1,    1);
GO

INSERT INTO PACIENTE (dni, nombres, apellidos, fecha_nacimiento, telefono, email, direccion, tipo_sangre, alergias) VALUES
    ('40123456', 'Ana',   'Quispe Flores', '1988-03-14', '999111222', 'ana.quispe@correo.pe', 'Jr. Los Álamos 120, VES',  'O+', NULL),
    ('41234567', 'Luis',  'Soto Mendoza',  '1975-11-02', '999111333', 'luis.soto@correo.pe',  'Av. Pachacútec 455, VES',  'A+', 'Penicilina'),
    ('42345678', 'Rosa',  'Lima Torres',   '1992-07-25', '999111444', NULL,                   'Av. Los Héroes 300, SJM',  NULL, NULL),
    ('43456789', 'Pedro', 'Ccori Huanca',  '2001-01-09', '999111555', NULL,                   NULL,                       NULL, NULL);

-- Los tres primeros pacientes ya aceptaron el tratamiento de sus datos; el cuarto aún no (no se le puede programar cita)
INSERT INTO CONSENTIMIENTO (id_paciente, finalidad, version_texto, fecha_aceptacion, canal)
SELECT id_paciente, 'Gestión de citas médicas y comunicaciones asociadas', '1.0', SYSDATETIME(), 'Presencial'
FROM PACIENTE WHERE dni IN ('40123456', '41234567', '42345678');
GO

SET NOEXEC OFF;
GO
PRINT 'Base BD_CitasMedicas creada. Ingrese con admin / Clave#2026';
GO


USE BD_CitasMedicas;
GO

SELECT username
FROM USUARIO;

USE BD_CitasMedicas;
GO

SELECT username,
       LEN(password_hash) AS LongitudHash
FROM USUARIO;

USE BD_CitasMedicas;
GO

SELECT COUNT(*) AS CantidadUsuarios
FROM USUARIO;