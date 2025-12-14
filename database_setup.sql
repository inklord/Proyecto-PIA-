-- Script de configuración de Base de Datos para AntMaster
-- Ejecuta este script en tu servidor MySQL local si deseas usar persistencia SQL.

CREATE DATABASE IF NOT EXISTS ev1;
USE ev1;

-- 1. Tabla de Usuarios
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(50) DEFAULT 'User',
    IsVerified BOOLEAN DEFAULT FALSE,
    VerificationCode VARCHAR(100),
    VerificationExpiresAt DATETIME
);

-- 2. Tabla de Especies
CREATE TABLE IF NOT EXISTS AntSpecies (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ScientificName VARCHAR(255) NOT NULL,
    AntWikiUrl VARCHAR(500),
    PhotoUrl VARCHAR(500),
    InaturalistId VARCHAR(100)
);

-- 3. Usuario Administrador por defecto (Pass: admin)
-- Nota: En producción las contraseñas deben estar hasheadas. 
-- Este proyecto usa un hash simple o texto plano para demostración académica.
INSERT INTO Users (Email, PasswordHash, Role, IsVerified) 
VALUES ('admin', 'admin', 'Admin', TRUE)
ON DUPLICATE KEY UPDATE Email=Email;

