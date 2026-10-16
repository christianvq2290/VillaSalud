// Confirmación antes de enviar formularios marcados con data-confirmar
document.addEventListener('submit', function (evento) {
    var mensaje = evento.target.getAttribute('data-confirmar');
    if (mensaje && !window.confirm(mensaje)) evento.preventDefault();
});
