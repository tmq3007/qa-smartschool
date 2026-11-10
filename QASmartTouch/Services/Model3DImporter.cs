using System;
using System.IO;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Assimp;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service for importing 3D models from various file formats
    /// </summary>
    public class Model3DImporter
    {
        private static readonly string[] SupportedFormats = { ".obj", ".fbx", ".3ds", ".stl", ".dae", ".blend" };
        
        /// <summary>
        /// Check if a file format is supported
        /// </summary>
        public bool IsValidFormat(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
                
            var extension = Path.GetExtension(filePath).ToLower();
            return Array.Exists(SupportedFormats, ext => ext == extension);
        }
        
        /// <summary>
        /// Get list of supported file formats
        /// </summary>
        public string[] GetSupportedFormats()
        {
            return (string[])SupportedFormats.Clone();
        }
        
        /// <summary>
        /// Load a 3D model from file using HelixToolkit
        /// </summary>
        public Model3DGroup? LoadModel(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[Model3DImporter] File not found: {filePath}");
                    return null;
                }
                
                if (!IsValidFormat(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[Model3DImporter] Unsupported format: {filePath}");
                    return null;
                }
                
                var extension = Path.GetExtension(filePath).ToLower();
                
                // HelixToolkit supports .obj, .3ds, .stl, .off directly
                if (extension == ".obj" || extension == ".3ds" || extension == ".stl" || extension == ".off")
                {
                    return LoadWithHelixToolkit(filePath);
                }
                // For other formats (.fbx, .dae, .blend), use Assimp
                else
                {
                    return LoadWithAssimp(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Model3DImporter] Error loading model: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Load model using HelixToolkit's built-in importers
        /// </summary>
        private Model3DGroup? LoadWithHelixToolkit(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".obj")
                {
                    var importer = new ObjReader();
                    return importer.Read(filePath);
                }
                else if (extension == ".stl")
                {
                    var importer = new StLReader();
                    return importer.Read(filePath);
                }
                else if (extension == ".3ds")
                {
                    var importer = new StudioReader();
                    return importer.Read(filePath);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Model3DImporter] HelixToolkit load error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Load model using Assimp and convert to WPF Model3DGroup
        /// </summary>
        private Model3DGroup? LoadWithAssimp(string filePath)
        {
            try
            {
                // Create Assimp context
                using (var importer = new AssimpContext())
                {
                    // Import the scene
                    var scene = importer.ImportFile(filePath,
                        PostProcessSteps.Triangulate |
                        PostProcessSteps.GenerateNormals |
                        PostProcessSteps.FlipUVs);
                    
                    if (scene == null || !scene.HasMeshes)
                    {
                        System.Diagnostics.Debug.WriteLine("[Model3DImporter] No meshes found in file");
                        return null;
                    }
                    
                    // Convert Assimp scene to WPF Model3DGroup
                    var modelGroup = new Model3DGroup();
                    
                    foreach (var mesh in scene.Meshes)
                    {
                        var geometry = ConvertAssimpMeshToWpf(mesh);
                        if (geometry != null)
                        {
                            var material = new DiffuseMaterial(System.Windows.Media.Brushes.LightGray);
                            var model = new GeometryModel3D(geometry, material);
                            modelGroup.Children.Add(model);
                        }
                    }
                    
                    return modelGroup;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Model3DImporter] Assimp load error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Convert Assimp mesh to WPF MeshGeometry3D
        /// </summary>
        private MeshGeometry3D? ConvertAssimpMeshToWpf(Assimp.Mesh assimpMesh)
        {
            try
            {
                var mesh = new MeshGeometry3D();
                
                // Add vertices
                foreach (var vertex in assimpMesh.Vertices)
                {
                    mesh.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
                }
                
                // Add normals
                if (assimpMesh.HasNormals)
                {
                    foreach (var normal in assimpMesh.Normals)
                    {
                        mesh.Normals.Add(new System.Windows.Media.Media3D.Vector3D(normal.X, normal.Y, normal.Z));
                    }
                }
                
                // Add texture coordinates
                if (assimpMesh.HasTextureCoords(0))
                {
                    foreach (var texCoord in assimpMesh.TextureCoordinateChannels[0])
                    {
                        mesh.TextureCoordinates.Add(new System.Windows.Point(texCoord.X, texCoord.Y));
                    }
                }
                
                // Add triangle indices
                foreach (var face in assimpMesh.Faces)
                {
                    if (face.IndexCount == 3)
                    {
                        mesh.TriangleIndices.Add(face.Indices[0]);
                        mesh.TriangleIndices.Add(face.Indices[1]);
                        mesh.TriangleIndices.Add(face.Indices[2]);
                    }
                }
                
                return mesh;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Model3DImporter] Mesh conversion error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get file filter string for OpenFileDialog
        /// </summary>
        public string GetFileFilter()
        {
            return "3D Models|*.obj;*.fbx;*.3ds;*.stl;*.dae;*.blend|" +
                   "Wavefront OBJ (*.obj)|*.obj|" +
                   "Autodesk FBX (*.fbx)|*.fbx|" +
                   "3D Studio (*.3ds)|*.3ds|" +
                   "Stereolithography (*.stl)|*.stl|" +
                   "COLLADA (*.dae)|*.dae|" +
                   "Blender (*.blend)|*.blend|" +
                   "All Files (*.*)|*.*";
        }
    }
}
