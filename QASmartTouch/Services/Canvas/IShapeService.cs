﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Services.Canvas
{
    /// <summary>
    /// Interface for shape creation and manipulation
    /// Handles geometric shape operations (rectangles, ellipses, lines, etc.)
    /// </summary>
    public interface IShapeService
    {
        /// <summary>
        /// Creates a rectangle shape
        /// </summary>
        /// <param name="bounds">The bounding rectangle</param>
        /// <param name="fillColor">The fill color</param>
        /// <param name="strokeColor">The stroke color</param>
        /// <param name="strokeThickness">The stroke thickness</param>
        /// <returns>A new Rectangle shape</returns>
        Rectangle CreateRectangle(Rect bounds, Color fillColor, Color strokeColor, double strokeThickness);
        
        /// <summary>
        /// Creates an ellipse shape
        /// </summary>
        /// <param name="bounds">The bounding rectangle</param>
        /// <param name="fillColor">The fill color</param>
        /// <param name="strokeColor">The stroke color</param>
        /// <param name="strokeThickness">The stroke thickness</param>
        /// <returns>A new Ellipse shape</returns>
        Ellipse CreateEllipse(Rect bounds, Color fillColor, Color strokeColor, double strokeThickness);
        
        /// <summary>
        /// Creates a line shape
        /// </summary>
        /// <param name="start">The start point</param>
        /// <param name="end">The end point</param>
        /// <param name="strokeColor">The stroke color</param>
        /// <param name="strokeThickness">The stroke thickness</param>
        /// <returns>A new Line shape</returns>
        Line CreateLine(Point start, Point end, Color strokeColor, double strokeThickness);
        
        /// <summary>
        /// Creates an arrow line shape
        /// </summary>
        /// <param name="start">The start point</param>
        /// <param name="end">The end point</param>
        /// <param name="strokeColor">The stroke color</param>
        /// <param name="strokeThickness">The stroke thickness</param>
        /// <returns>A Path shape representing an arrow</returns>
        System.Windows.Shapes.Path CreateArrow(Point start, Point end, Color strokeColor, double strokeThickness);
        
        /// <summary>
        /// Resizes a shape to new bounds
        /// </summary>
        /// <param name="shape">The shape to resize</param>
        /// <param name="newBounds">The new bounding rectangle</param>
        void ResizeShape(Shape shape, Rect newBounds);
        
        /// <summary>
        /// Moves a shape to a new position
        /// </summary>
        /// <param name="shape">The shape to move</param>
        /// <param name="newPosition">The new top-left position</param>
        void MoveShape(Shape shape, Point newPosition);
        
        /// <summary>
        /// Rotates a shape around its center
        /// </summary>
        /// <param name="shape">The shape to rotate</param>
        /// <param name="angleDegrees">The rotation angle in degrees</param>
        void RotateShape(Shape shape, double angleDegrees);
        
        /// <summary>
        /// Gets the bounding box of a shape
        /// </summary>
        /// <param name="shape">The shape to get bounds for</param>
        /// <returns>The bounding rectangle</returns>
        Rect GetShapeBounds(Shape shape);
    }
}
