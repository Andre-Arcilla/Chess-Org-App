import { createClient } from '@supabase/supabase-js'

const supabaseUrl = import.meta.env.VITE_SUPABASE_URL;
const supabaseKey = import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY;

// Every query MUST explicitly chain .schema('Chessistant') as per requirements
export const supabase = createClient(supabaseUrl, supabaseKey);
