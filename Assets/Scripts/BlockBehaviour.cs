using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockBehaviour
{
    /*public static bool Active (VoxelState voxel) {

        switch (voxel.id) {
            case VoxelData.grassBlockId: // Grass
                if ((voxel.neighbours[0] != null && voxel.neighbours[0].id == VoxelData.dirtBlockId) ||
                    (voxel.neighbours[1] != null && voxel.neighbours[1].id == VoxelData.dirtBlockId) ||
                    (voxel.neighbours[4] != null && voxel.neighbours[4].id == VoxelData.dirtBlockId) ||
                    (voxel.neighbours[5] != null && voxel.neighbours[5].id == VoxelData.dirtBlockId)) {
                    return true;
                }
                break;
        }
        

        // If we get here, the block either isn't active or doesn't have a behaviour. Just return false.
        return false;
    }

    public static void Behave (VoxelState voxel) {

        switch (voxel.id) {

            case VoxelData.grassBlockId: // Grass
                var topId = voxel.neighbours[2].id;
                if (voxel.neighbours[2] != null && topId != 0) {
                    voxel.chunkData.chunk.RemoveActiveVoxel(voxel);
                    voxel.chunkData.ModifyVoxel(voxel.position, VoxelData.dirtBlockId, 0);
                    return;
                }

                var neighbours = new List<VoxelState>();
                if ((voxel.neighbours[0] != null && voxel.neighbours[0].id == VoxelData.dirtBlockId)) neighbours.Add(voxel.neighbours[0]);
                if ((voxel.neighbours[1] != null && voxel.neighbours[1].id == VoxelData.dirtBlockId)) neighbours.Add(voxel.neighbours[1]);
                if ((voxel.neighbours[4] != null && voxel.neighbours[4].id == VoxelData.dirtBlockId)) neighbours.Add(voxel.neighbours[4]);
                if ((voxel.neighbours[5] != null && voxel.neighbours[5].id == VoxelData.dirtBlockId)) neighbours.Add(voxel.neighbours[5]);

                if (neighbours.Count == 0) return;

                var index = Random.Range(0, neighbours.Count);
                neighbours[index].chunkData.ModifyVoxel(neighbours[index].position, VoxelData.grassBlockId, 0);

                break;

        }
    }*/
}
