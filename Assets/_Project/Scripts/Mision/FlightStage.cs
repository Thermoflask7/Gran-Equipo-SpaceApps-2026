// Lo que muestra la vista 3D (mitad derecha) de 02_Lanzamiento. Ver LaunchScreen.
//   Liftoff: acercamiento, la camara sigue al cohete despegando y saliendo de la atmosfera (inicio de la fase Salida).
//   Transit: vista general de la ruta Tierra -> Luna -> Tierra (resto de Salida, Orbita lunar y la primera parte de Retorno).
//   Landing: acercamiento, la camara sigue a la capsula en la reentrada hasta el amerizaje (final de la fase Retorno).
public enum FlightStage
{
    Liftoff,
    Transit,
    Landing
}
