from collections import namedtuple

Point = namedtuple('Point', 'x y z')

def get_neighbours(p):
    return [Point(p.x + 1, p.y, p.z), Point(p.x - 1, p.y, p.z), Point(p.x, p.y + 1, p.z), Point(p.x, p.y - 1, p.z), Point(p.x, p.y, p.z + 1), Point(p.x, p.y, p.z - 1)]

origin = Point(0, 0, 0)
points = [origin]
last_level = [origin]
connections = []

for level in range(0, 2):
    this_level = []
    for point in last_level:
        neighbours = get_neighbours(point)
        this_level.extend(neighbours)
        connections.append((point, neighbours))
    points.extend(this_level)
    last_level = this_level
    
distinct_points = list(set(points))
indexed_connections = []
for connection in connections:
    i = distinct_points.index(connection[0])
    c = [distinct_points.index(p) for p in connection[1]]
    indexed_connections.append((i, c))

indexed_connections.reverse()
flattened_connections = [[conn[0]] + conn[1] for conn in indexed_connections]
flattened_connections = [point for sublist in flattened_connections for point in sublist]

distinct_points_str = ', '.join([f"ivec3({p.x}, {p.y}, {p.z})" for p in distinct_points])

print(len(distinct_points))
print(distinct_points_str)
print(len(flattened_connections))
print(flattened_connections)