(() => {
    const formulario = document.getElementById("form-invitar");
    if (!formulario) return;

    const FORMATO_CORREO = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
    const correo = document.getElementById("correo");
    const token = formulario.querySelector('input[name="__RequestVerificationToken"]').value;
    let temporizador;
    let correoValido = true;

    async function validar() {
        const valor = correo.value.trim();
        if (!valor) return;
        if (!FORMATO_CORREO.test(valor)) {
            correoValido = false;
            FirmaYa.marcarCampo(correo, "Ingrese un correo electrónico válido (ej. nombre@dominio.com)");
            return;
        }
        try {
            const datos = new FormData();
            datos.append("correoElectronico", valor);
            datos.append("__RequestVerificationToken", token);
            const respuesta = await fetch(formulario.dataset.validarCorreo, { method: "POST", body: datos, credentials: "same-origin" });
            const resultado = await respuesta.json();
            correoValido = resultado.valido;
            FirmaYa.marcarCampo(correo, resultado.mensaje);
        } catch {
            correoValido = true; // la validación definitiva la hace el servidor al enviar
        }
    }

    correo.addEventListener("input", () => {
        clearTimeout(temporizador);
        temporizador = setTimeout(validar, 400);
    });
    correo.addEventListener("blur", validar);

    formulario.addEventListener("submit", (evento) => {
        if (!correoValido) {
            evento.preventDefault();
            correo.focus();
        }
    });
})();
