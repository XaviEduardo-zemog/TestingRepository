window.vzInterop = {
    // Un solo listener global (igual que PR_ en el HTML original: "un solo keydown que solo
    // actúa si presView está visible"). Se registra UNA vez por página; el filtro de "¿está
    // abierta la presentación?" vive en C# (ResumenPresentacion.razor.OnTeclaPresionada), no aquí.
    iniciarTeclado: function (dotNetRef) {
        // Reemplaza cualquier listener previo en vez de "registrar una sola vez para siempre":
        // así un dotNetRef ya Dispose()-ado (componente recreado) nunca se queda colgado.
        if (window.vzTecladoListener) document.removeEventListener("keydown", window.vzTecladoListener);

        window.vzTecladoListener = function (e) {
            const teclas = ["ArrowRight", "ArrowLeft", "Escape", " ", "PageDown", "PageUp"];
            if (teclas.indexOf(e.key) === -1) return;
            // Solo bloquea el scroll/paginado nativo del navegador (Space/PageDown/PageUp)
            // cuando la presentación está realmente abierta -- si no, estas teclas deben
            // seguir funcionando normal en el resto de la página.
            if (document.querySelector(".pres-overlay.pres-abierta")) e.preventDefault();
            dotNetRef.invokeMethodAsync("OnTeclaPresionada", e.key);
        };
        document.addEventListener("keydown", window.vzTecladoListener);
    },

    removerTeclado: function () {
        if (window.vzTecladoListener) {
            document.removeEventListener("keydown", window.vzTecladoListener);
            window.vzTecladoListener = null;
        }
    },

    // Cierra el panel de filtro abierto (MultiSelectFiltro.razor, clase "ms-filtro") al hacer clic
    // fuera de él -- mismo comportamiento que el listener global de viajes_v14.html (L839-845).
    // Un solo listener global, reemplazado en cada registro (igual criterio que iniciarTeclado).
    iniciarClickFuera: function (dotNetRef) {
        if (window.vzClickFueraListener) document.removeEventListener("click", window.vzClickFueraListener);

        window.vzClickFueraListener = function (e) {
            if (e.target.closest(".ms-filtro")) return;
            dotNetRef.invokeMethodAsync("CerrarFiltroPorClickFuera");
        };
        document.addEventListener("click", window.vzClickFueraListener);
    },

    removerClickFuera: function () {
        if (window.vzClickFueraListener) {
            document.removeEventListener("click", window.vzClickFueraListener);
            window.vzClickFueraListener = null;
        }
    },

    // Blob + <a download> -- el mismo truco que RE_exportWord (L1564-1604 de viajes_v14.html),
    // reutilizado tal cual porque es el mecanismo más simple para bajar un archivo generado en
    // el servidor sin un endpoint HTTP nuevo. contenidoBase64 llega ya codificado desde C#
    // (Convert.ToBase64String) porque la conexión SignalR de Blazor Server transporta JSON.
    descargarArchivo: function (nombre, tipoMime, contenidoBase64) {
        const binario = atob(contenidoBase64);
        const bytes = new Uint8Array(binario.length);
        for (let i = 0; i < binario.length; i++) bytes[i] = binario.charCodeAt(i);

        const blob = new Blob([bytes], { type: tipoMime });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = nombre;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    // Persistencia del tema (localStorage) -- misma clave que el HTML original (viajesTema) para
    // que la elección sobreviva a un F5. "Persistir si resulta sencillo": con el resto del
    // archivo ya necesario para teclado/descarga, agregar esto es trivial.
    guardarTema: function (tema) {
        try { localStorage.setItem("viajesTema", tema); } catch { /* almacenamiento no disponible (modo privado, etc.) -- no es crítico */ }
    },

    leerTema: function () {
        try { return localStorage.getItem("viajesTema"); } catch { return null; }
    },

    // Botón flotante "volver arriba" (Fase 10, punto 6) -- scroll suave nativo, sin nada que
    // replicar del original más allá de esto (#up en viajes_v14.html hace exactamente lo mismo).
    scrollArriba: function () {
        window.scrollTo({ top: 0, behavior: "smooth" });
    },

    // Tarjetas KPI arrastrables (viajes_v14.html L1902-1941): Drag and Drop API nativa de HTML5,
    // sin librería. El orden reordenado vive solo en el DOM de la sesión -- nunca se persiste en
    // localStorage (el original es explícito: "orden solo de la sesión"), así que al recargar la
    // página vuelve al orden original que produce el @foreach de Blazor.
    // Idempotente: se invoca tras cada render (OnAfterRenderAsync corre en cada render, no solo en
    // firstRender, porque .vz-kpis se repinta cuando cambia kpisComparativa), así que cada hijo ya
    // activado se marca con dataset.dnd="1" para no volver a asignarle listeners duplicados.
    activarKpisArrastrables: function () {
        const contenedor = document.querySelector(".vz-kpis");
        if (!contenedor) return;

        // window.vzKpiArrastrado (no una variable local a esta llamada): activarKpisArrastrables
        // es idempotente y se invoca una vez por cada hijo nuevo (dataset.dnd), así que si las
        // tarjetas llegaran a activarse en invocaciones distintas, cada una necesita ver el mismo
        // estado "qué se está arrastrando ahora mismo" -- igual criterio que vzClickFueraListener.
        Array.from(contenedor.children).forEach(function (hijo) {
            if (hijo.dataset.dnd === "1") return;
            hijo.dataset.dnd = "1";

            hijo.draggable = true;
            hijo.style.cursor = "grab";
            hijo.title = "Arrastra para reordenar";

            hijo.addEventListener("dragstart", function () {
                window.vzKpiArrastrado = hijo;
                hijo.style.opacity = ".35";
            });

            hijo.addEventListener("dragend", function () {
                hijo.style.opacity = "";
                window.vzKpiArrastrado = null;
            });

            hijo.addEventListener("dragover", function (e) {
                e.preventDefault();
            });

            hijo.addEventListener("drop", function (e) {
                e.preventDefault();
                const arrastrado = window.vzKpiArrastrado;
                if (!arrastrado || arrastrado === hijo) return;

                const rect = hijo.getBoundingClientRect();
                const antes = (e.clientX - rect.left) < (rect.width / 2);
                contenedor.insertBefore(arrastrado, antes ? hijo : hijo.nextSibling);
            });
        });
    },
};