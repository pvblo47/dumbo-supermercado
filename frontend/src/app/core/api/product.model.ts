export interface Product {
  id: number;
  sku: string;
  name: string;
  category: string;
  presentation: string;
  price: number;
  stock: number;
  imageUrl: string | null;
}
