using System.Runtime.CompilerServices;

// Permite que Testing.Application.Tests llame directamente a los métodos internal de matching
// de Folio (LimpiarFolio/CandidatoSlash/QuitarSufijo/ConstruirCandidatos en
// GetViajesQueryHandler) sin exponerlos como API pública del ensamblado.
[assembly: InternalsVisibleTo("Testing.Application.Tests")]